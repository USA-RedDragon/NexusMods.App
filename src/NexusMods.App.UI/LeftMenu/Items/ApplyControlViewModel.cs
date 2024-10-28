using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Exceptions;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
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
    private readonly IOverlayController _overlayController;
    private readonly GameInstallMetadataId _gameMetadataId;
    [Reactive] private bool CanApply { get; set; } = true;

    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }

    [Reactive] public string ApplyButtonText { get; private set; } = Language.ApplyControlViewModel__APPLY;
    [Reactive] public bool IsLaunchButtonEnabled { get; private set; } = true;

    public ILaunchButtonViewModel LaunchButtonViewModel { get; }

    public ApplyControlViewModel(LoadoutId loadoutId, IServiceProvider serviceProvider, IJobMonitor jobMonitor, IOverlayController overlayController)
    {
        _loadoutId = loadoutId;
        _overlayController = overlayController;
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
                var isGameRunning = jobMonitor.GetObservableChangeSet<IRunGameTool>()
                    .TransformOnObservable(job => job.ObservableStatus)
                    .Select(_ =>
                        {
                            // TODO: We don't currently remove any old/stale jobs, this could be more efficient - sewer
                            return jobMonitor.Jobs.Any(x => x is { Definition: IRunGameTool, Status: JobStatus.Running });
                        }
                    )
                    .DistinctUntilChanged()
                    .StartWith(jobMonitor.Jobs.Any(x => x is { Definition: IRunGameTool, Status: JobStatus.Running }));
                
                // Note(sewer):
                // Fire an initial value with StartWith because CombineLatest requires all stuff to have latest values.
                // Note: This observable is a bit complex. We can't just start listening to games closed or
                //       new games started in isolation because we don't know the initial state here.
                //      
                //       For example, assume we listen only to started jobs. If a game is already running and the
                //       user navigates to another loadout, then the 'isGameRunning' observable will be initialized with
                //       `true` as initial state. However, because there is no prior state, closing the game will not yield
                //       `false`.
                //
                // In any case, we should prevent Apply from being available while a file is in use.
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
        try
        {
            await Task.Run(async () =>
            {
                await _syncService.Synchronize(_loadoutId);
            });
        }
        catch (ExecutableInUseException)
        {
            await MessageBoxOkViewModel.ShowGameAlreadyRunningError(_overlayController);
        }
    }
}
