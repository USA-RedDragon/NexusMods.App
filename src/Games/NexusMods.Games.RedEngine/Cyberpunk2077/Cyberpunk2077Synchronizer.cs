using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class Cyberpunk2077Synchronizer : ALoadoutSynchronizer
{
    private Cyberpunk2077Settings _settings;
    
    /// <summary>
    /// Redmod deploys combined mods to the redmod cache folder
    /// </summary>
    private static GamePath RedModCacheFolder => new(LocationId.Game, "r6/cache/modded");
    
    /// <summary>
    /// Redmod stages the scripts in the redmod/scripts folder
    /// </summary>
    private static GamePath RedModScriptsFolder => new(LocationId.Game, "tools/redmod/scripts");
    
    /// <summary>
    /// Redmod stages the tweaks in the redmod/tweaks folder
    /// </summary>
    private static GamePath RedModTweaksFolder => new(LocationId.Game, "tools/redmod/tweaks");
    
    
    private readonly RedModDeployTool _redModTool;
    
    protected internal Cyberpunk2077Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();

        _settings = settingsManager.Get<Cyberpunk2077Settings>();
        settingsManager.GetChanges<Cyberpunk2077Settings>().Subscribe(value => _settings = value);
        _redModTool = provider.GetServices<ITool>().OfType<RedModDeployTool>().First();
    }

    private static readonly GamePath[] IgnoredBackupFolders =
    [
        new GamePath(LocationId.Game, "archive/pc/content"),
        new GamePath(LocationId.Game, "archive/pc/ep1"),
    ];
    
    public override bool IsIgnoredPath(GamePath path)
    {
        // Ignore the mod cache folder, as it's regenerated by redmod every time we deploy using the tool.
        return path.InFolder(RedModCacheFolder)
            || path.InFolder(RedModScriptsFolder)
            || path.InFolder(RedModTweaksFolder);
    }

    public override async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        loadout = await base.Synchronize(loadout);
        var hasRedMods = RedModInfoFile.All(loadout.Db)
            .Any(l => l.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().LoadoutId == loadout.LoadoutId);
        
        if (!hasRedMods) 
            return loadout;
        
        if (!OSInformation.Shared.IsWindows)
        {
            // TODO: run redmod on non-Windows systems (https://github.com/Nexus-Mods/NexusMods.App/issues/308)
            return loadout;
        }

        await _redModTool.Execute(loadout, CancellationToken.None);
        return await base.Synchronize(loadout);

    }


    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup)
            return false;
        
        if (path.LocationId != LocationId.Game)
            return false;
        
        return IgnoredBackupFolders.Any(ignore => path.Path.InFolder(ignore.Path));
    }

}
