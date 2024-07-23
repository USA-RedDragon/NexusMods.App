using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns;

public abstract class AColumnViewModel<TBaseInterface, TValue> : AViewModel<TBaseInterface>, IComparableColumn<ModId>,
    INotifyPropertyChanged
    where TBaseInterface : class, IViewModelInterface, ICellViewModel<TValue>
{
    private readonly IConnection _conn;

    protected AColumnViewModel(IConnection conn)
    {
        _conn = conn;
        
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(id => Mod.Observe(_conn, id))
                .Select(Selector)
                .OnUI()
                .BindTo(this, vm => vm.Value)
                .DisposeWith(d);
        });
        
    }

    /// <summary>
    /// A selector function to get the value of the column from the model
    /// </summary>
    protected abstract TValue Selector(Mod.ReadOnly model);
    
    /// <summary>
    /// A comparer function to compare two values of the column
    /// </summary>
    protected abstract int Compare(TValue a, TValue b);
    
    
    private ModId _row = default!;

    /// <summary>
    /// The Source ModId
    /// </summary>
    public ModId Row
    {
        get => _row;
        set => this.RaiseAndSetIfChanged(ref _row, value);
    }

    
    private TValue _value = default!;
    
    /// <summary>
    /// The Value of the Column
    /// </summary>
    public TValue Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }
    
    public int Compare(ModId a, ModId b)
    {
        var db = _conn.Db;
        var aEnt = Mod.Load(db, a);
        var bEnt = Mod.Load(db, b);
        return Compare(Selector(aEnt), Selector(bEnt));
    }
}
