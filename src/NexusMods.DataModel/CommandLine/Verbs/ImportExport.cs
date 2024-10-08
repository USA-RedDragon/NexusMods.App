using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.DataModel.CommandLine.Verbs;

public static class ImportExport
{
    public static IServiceCollection AddImportExportVerbs(this IServiceCollection services) =>
        services
            .AddVerb(() => Export);
    
    [Verb("export-datamodel", "Export the contents of the database to a file")]
    public static async Task<int> Export([Injected] IRenderer renderer, 
        [Injected] IConnection connection,
        [Option("f", "file", "File to export to, file will be compressed using deflate")] AbsolutePath file)
    {
        await using var stream = file.Create();
        await using var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal);
        await renderer.RenderAsync(Renderable.Text("Exporting database to file"));
        await connection.DatomStore.ExportAsync(deflateStream);
        await renderer.RenderAsync(Renderable.Text("Export complete"));
        return 0;
        
    }
}
