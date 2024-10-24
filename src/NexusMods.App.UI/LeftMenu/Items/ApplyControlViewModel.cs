using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class ApplyControlViewModel : AViewModel<IApplyControlViewModel>, IApplyControlViewModel
{
    private readonly IConnection _conn;
    private readonly ISynchronizerService _syncService;
    private readonly IJobMonitor _jobMonitor;

    private readonly LoadoutId _loadoutId;
    private readonly GameInstallMetadataId _gameMetadataId;
    [Reactive] private bool CanApply { get; set; } = true;

    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;
    [Reactive] public bool IsLaunchButtonEnabled { get; private set; } = true;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider, IJobMonitor jobMonitor)
    {
        _loadoutId = loadoutId;
        _syncService = serviceProvider.GetRequiredService<ISynchronizerService>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        var windowManager = serviceProvider.GetRequiredService<IWindowManager>();
        
        _gameMetadataId = NexusMods.Abstractions.Loadouts.Loadout.Load(_conn.Db, loadoutId).InstallationId;

        LaunchButtonViewModel = serviceProvider.GetRequiredService<ILaunchButtonViewModel>();
        LaunchButtonViewModel.LoadoutId = loadoutId;
        
        ApplyCommand = ReactiveCommand.CreateFromTask(async () => await Apply(), 
            canExecute: this.WhenAnyValue(vm => vm.CanApply));
        
        ShowApplyDiffCommand = ReactiveCommand.Create<NavigationInformation>(info =>
        {
            var pageData = new PageData
            {
                FactoryId = ApplyDiffPageFactory.StaticId,
                Context = new ApplyDiffPageContext
                {
                    LoadoutId = _loadoutId,
                },
            };

            var workspaceController = windowManager.ActiveWorkspaceController;

            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            var workspaceId = workspaceController.ActiveWorkspaceId;
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });

        this.WhenActivated(disposables =>
            {
                var loadoutStatuses = Observable.FromAsync(() => _syncService.StatusForLoadout(_loadoutId))
                    .Switch();

                var gameStatuses = _syncService.StatusForGame(_gameMetadataId);
                var isGameRunning = jobMonitor.ObserveActiveJobs<IRunGameTool>()
                    .Count()
                    .Select(x => x > 0)
                    .StartWith(false); // fire an initial value because CombineLatest requires all stuff to have latest values.
                
                // We should prevent Apply from being available while a file is in use.
                // A file may be in use because:
                // - The user launched the game externally (e.g. through Steam).
                //     - Approximate this by seeing if any EXE in any of the game folders are running.
                //     - This is done in 'Synchronize' method.
                // - They're running a tool from within the App.
                //     - Check running jobs.
                loadoutStatuses.CombineLatest(gameStatuses, isGameRunning, (loadout, game, running) => (loadout, game, running))
                    .OnUI()
                    .Subscribe(status =>
                    {
                        var (ldStatus, gameStatus, running) = status;

                        CanApply = gameStatus != GameSynchronizerState.Busy
                                   && ldStatus != LoadoutSynchronizerState.Pending
                                   && ldStatus != LoadoutSynchronizerState.Current
                                   && !running;
                        IsLaunchButtonEnabled = ldStatus == LoadoutSynchronizerState.Current && gameStatus != GameSynchronizerState.Busy && !running;
                    })
                    .DisposeWith(disposables);
            }
        );
    }

    private async Task Apply()
    {
        await Task.Run(async () =>
        {
            await _syncService.Synchronize(_loadoutId);
        });
    }
}
