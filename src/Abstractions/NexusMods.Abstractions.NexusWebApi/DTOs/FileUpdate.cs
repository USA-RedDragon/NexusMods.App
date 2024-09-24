using System.Text.Json.Serialization;
using NexusMods.Abstractions.NexusWebApi.Types;
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// This represents an entry which declares which
/// </summary>
public class FileUpdate
{
    /// <summary>
    /// Unique [per-game] ID for the file which has an update.
    /// </summary>
    [JsonPropertyName("old_file_id")]
    public FileId OldFileId { get; set; }
    
    /// <summary>
    /// Unique [per-game] ID for the newer version of the file with <see cref="OldFileId"/>.
    /// </summary>
    [JsonPropertyName("new_file_id")]
    public FileId NewFileId { get; set; }

    /// <summary>
    /// Name of the old file with <see cref="OldFileId"/>
    /// </summary>
    [JsonPropertyName("old_file_name")]
    public string OldFileName { get; set; } = "";

    /// <summary>
    /// Name of the new file with <see cref="NewFileId"/>
    /// </summary>
    [JsonPropertyName("new_file_name")]
    public string NewFileName { get; set; } = "";

    /// <summary>
    /// Unix timestamp for the time this file was uploaded.
    /// </summary>
    [JsonPropertyName("uploaded_timestamp")]
    public long UploadedTimestamp { get; set; }

    /// <summary>
    /// Specifies when this mod was uploaded.
    /// This is equivalent to <see cref="UploadedTimestamp"/>.
    /// </summary>
    /// <remarks>
    ///    Expressed as ISO 8601 compatible date/time notation.
    /// </remarks>
    [JsonPropertyName("uploaded_time")]
    public DateTime UploadedTime { get; set; }
}
