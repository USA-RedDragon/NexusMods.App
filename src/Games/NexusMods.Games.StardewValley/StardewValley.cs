using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

[UsedImplicitly]
public class StardewValley : AGame, ISteamGame, IGogGame
{
    private readonly IFileSystem _fileSystem;

    public IEnumerable<int> SteamIds => new[] { 413150 };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };

    public override string Name => "Stardew Valley";

    public static GameDomain GameDomain => GameDomain.From("stardewvalley");
    public override GameDomain Domain => GameDomain;

    public StardewValley(IFileSystem fileSystem, IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
        _fileSystem = fileSystem;
    }

    public override GamePath PrimaryFile
    {
        get
        {
            // "StardewValley" is a wrapper shell script that launches the "Stardew Valley" binary
            // on OSX, it's used to check the .NET version, on Linux it just launches the game
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new GamePath(GameFolderType.Game, "StardewValley");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new GamePath(GameFolderType.Game,"Contents/MacOS/StardewValley");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: SMAPI adds StardewModdingAPI.exe, which should be launched instead
                return new GamePath(GameFolderType.Game, "Stardew Valley.exe");
            }

            throw new PlatformNotSupportedException();
        }
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);

        var stardewValleyAppDataPath = _fileSystem
            .GetKnownPath(KnownPath.ApplicationDataDirectory)
            .CombineUnchecked("StardewValley");

        // base game saves
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(
            GameFolderType.Saves,
            stardewValleyAppDataPath.CombineUnchecked("Saves")
        );

        // TODO: SMAPI saves
        //yield return new KeyValuePair<GameFolderType, AbsolutePath>(
        //    GameFolderType.Saves,
        //    stardewValleyAppDataPath.CombineUnchecked(".smapi")
        //);
    }

    public override IStreamFactory Icon => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.icon.png");

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.game_image.jpg");
}
