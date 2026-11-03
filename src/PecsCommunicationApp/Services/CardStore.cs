using System.Text.Json;
using PecsCommunicationApp.Models;
using SQLite;

namespace PecsCommunicationApp.Services;

public sealed class CardStore
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _initialized;

    public CardStore()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "pecs-communication.db3");
        _database = new SQLiteAsyncConnection(path);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initializeLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await _database.CreateTableAsync<CommunicationCard>();
            if (await _database.Table<CommunicationCard>().CountAsync() == 0)
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync("cards.json");
                var cards = await JsonSerializer.DeserializeAsync<List<CommunicationCard>>(stream)
                    ?? new List<CommunicationCard>();
                await _database.InsertAllAsync(cards);
            }

            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public async Task<List<CommunicationCard>> GetAllAsync()
    {
        await InitializeAsync();
        return await _database.Table<CommunicationCard>()
            .OrderBy(card => card.SortOrder)
            .ToListAsync();
    }

    public async Task SaveAsync(CommunicationCard card)
    {
        await InitializeAsync();
        await _database.InsertOrReplaceAsync(card);
    }

    public async Task DeleteAsync(CommunicationCard card)
    {
        await InitializeAsync();
        await _database.DeleteAsync(card);
    }
}
