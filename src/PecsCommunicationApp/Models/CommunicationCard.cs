using SQLite;

namespace PecsCommunicationApp.Models;

[Table("Cards")]
public sealed class CommunicationCard
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string LabelEn { get; set; } = string.Empty;
    public string LabelRu { get; set; } = string.Empty;
    public string CategoryId { get; set; } = "needs";
    public string Symbol { get; set; } = "🖼️";
    public string? ImagePath { get; set; }
    public string? AudioEnPath { get; set; }
    public string? AudioRuPath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    [Ignore]
    public string? ActiveAudioPath { get; set; }

    public string LabelFor(string language) => language == "ru" ? LabelRu : LabelEn;
    public string AudioFor(string language) => language == "ru" ? AudioRuPath ?? string.Empty : AudioEnPath ?? string.Empty;
}
