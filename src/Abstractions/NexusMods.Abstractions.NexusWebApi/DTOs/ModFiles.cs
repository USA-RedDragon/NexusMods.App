using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;
using NexusMods.Abstractions.NexusWebApi.Types;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Represents the collection of downloadable files tied to a mod page.
/// </summary>
public class ModFiles : IJsonSerializable<ModFiles>
{
    /// <summary/>
    [JsonPropertyName("files")]
    public ModFile[] Files { get; set; }
    
    /// <summary>
    /// A list of updates to individual files on the given mod page.
    /// Use this to find updates to given files.
    /// </summary>
    /// <remarks>
    ///     The updates are sorted in chronological order in the responses,
    ///     so a fast way to find the most recent update for a given file
    ///     is to walk linearly; however; we should not rely on this implementation
    ///     detail as it's not noted in the API contract.
    /// </remarks>
    public FileUpdate[] FileUpdates { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<ModFiles> GetTypeInfo() => ModFilesContext.Default.ModFiles;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModFiles))]
public partial class ModFilesContext : JsonSerializerContext { }
