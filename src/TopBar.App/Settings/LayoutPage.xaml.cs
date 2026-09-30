using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Settings;
using Windows.ApplicationModel.DataTransfer;

namespace TopBar.App.SettingsPages;

/// <summary>
/// Arrange the bar elements: three zone lists plus "not shown". Move by drag and drop or with the buttons.
/// Every change is saved immediately and the bars rebuild themselves.
/// </summary>
public sealed partial class LayoutPage : Page
{
    private sealed record Item(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    private readonly record struct Sel(ListView List, Item Item, int ZoneIndex);

    private readonly PageState _s = new();
    private readonly ListView[] _lists;   // index 0..2 = BarZone, 3 = not shown
    private bool _updatingSelection;

    public LayoutPage()
    {
        InitializeComponent();
        _lists = [LeftList, CenterList, RightList, HiddenList];

        for (var i = 0; i < _lists.Length; i++)
        {
            var list = _lists[i];
            var zone = i < 3 ? (BarZone?)(BarZone)i : null;
            list.CanDragItems = true;
            list.AllowDrop = true;
            list.SelectionChanged += (_, _) => OnSelectionChanged(list);
            list.DragItemsStarting += (_, e) =>
            {
                if (e.Items.FirstOrDefault() is Item item)
                {
                    e.Data.SetText(item.Id);
                    e.Data.RequestedOperation = DataPackageOperation.Move;
                }
            };
            list.DragOver += (_, e) => e.AcceptedOperation = DataPackageOperation.Move;
            list.Drop += async (_, e) =>
            {
                e.Handled = true;
                if (!e.DataView.Contains(StandardDataFormats.Text)) return;
                try
                {
                    var id = await e.DataView.GetTextAsync();
                    Move(id, zone, IndexAt(list, e.GetPosition(list).Y));
                }
                catch (Exception ex)
                {
                    AppController.LogWarning(ex, "Layout: drop failed");
                }
            };
        }

        Loaded += (_, _) => _s.Load(() => Reload(null));
    }

    /// <summary>Rebuilds all four lists from the settings and re-selects <paramref name="selectId"/> if given.</summary>
    private void Reload(string? selectId)
    {
        var settings = _s.Settings.Current;
        _updatingSelection = true;
        try
        {
            Fill(LeftList, settings.Layout.Left, settings);
            Fill(CenterList, settings.Layout.Center, settings);
            Fill(RightList, settings.Layout.Right, settings);
            Fill(HiddenList, settings.Layout.Unplaced(), settings);

            if (selectId is not null)
            {
                foreach (var list in _lists)
                {
                    var match = list.Items.OfType<Item>().FirstOrDefault(i => i.Id == selectId);
                    if (match is not null) { list.SelectedItem = match; break; }
                }
            }
        }
        finally
        {
            _updatingSelection = false;
        }
        UpdateButtons();
    }

    private static void Fill(ListView list, IEnumerable<string> ids, AppSettings settings)
    {
        list.Items.Clear();
        foreach (var id in ids)
        {
            var name = BarElements.Name(id);
            if (!BarElements.IsEnabled(settings, id)) name += " " + Loc.Get("Layout_Switched_Off");
            list.Items.Add(new Item(id, name));
        }
    }

    private void OnSelectionChanged(ListView source)
    {
        if (_updatingSelection || source.SelectedItem is null) return;
        _updatingSelection = true;
        try
        {
            foreach (var other in _lists.Where(l => !ReferenceEquals(l, source))) other.SelectedItem = null;
        }
        finally
        {
            _updatingSelection = false;
        }
        UpdateButtons();
    }

    private Sel? Selected()
    {
        for (var i = 0; i < _lists.Length; i++)
            if (_lists[i].SelectedItem is Item item) return new Sel(_lists[i], item, i);
        return null;
    }

    private void UpdateButtons()
    {
        if (Selected() is not { } sel)
        {
            EarlierButton.IsEnabled = LaterButton.IsEnabled = false;
            ToLeftButton.IsEnabled = ToCenterButton.IsEnabled = ToRightButton.IsEnabled = false;
            HideButton.IsEnabled = false;
            return;
        }

        var inZone = sel.ZoneIndex < 3;
        EarlierButton.IsEnabled = inZone && sel.List.SelectedIndex > 0;
        LaterButton.IsEnabled = inZone && sel.List.SelectedIndex < sel.List.Items.Count - 1;
        ToLeftButton.IsEnabled = sel.ZoneIndex != (int)BarZone.Left;
        ToCenterButton.IsEnabled = sel.ZoneIndex != (int)BarZone.Center;
        ToRightButton.IsEnabled = sel.ZoneIndex != (int)BarZone.Right;
        HideButton.IsEnabled = inZone;
    }

    /// <summary>Index of the item before which a drop at vertical position <paramref name="y"/> (relative to the list) inserts.</summary>
    private static int IndexAt(ListView list, double y)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.ContainerFromIndex(i) is not FrameworkElement container) continue;
            var top = container.TransformToVisual(list).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
            if (y < top + container.ActualHeight / 2) return i;
        }
        return list.Items.Count;
    }

    /// <summary>Moves <paramref name="id"/> into <paramref name="zone"/> (null = not shown) at <paramref name="index"/>.</summary>
    private void Move(string id, BarZone? zone, int index)
    {
        if (!BarElementIds.All.Contains(id)) return;

        // Dropping into the same zone behind its own old position: the removal shifts the index by one.
        var layout = _s.Settings.Current.Layout;
        if (zone is { } z)
        {
            var old = layout.Zone(z).IndexOf(id);
            if (old >= 0 && old < index) index--;
        }

        _s.Save(x =>
        {
            if (zone is { } target)
            {
                x.Layout.Place(id, target, index);
                BarElements.Enable(x, id); // placing an element always makes it appear
            }
            else
            {
                x.Layout.Remove(id);
            }
        });
        Reload(id);
    }

    private void MoveSelectedWithinZone(int delta)
    {
        if (Selected() is not { ZoneIndex: < 3 } sel) return;
        var index = sel.List.SelectedIndex + delta;
        if (index < 0 || index >= sel.List.Items.Count) return;
        var id = sel.Item.Id;
        var zone = (BarZone)sel.ZoneIndex;
        _s.Save(x => x.Layout.Place(id, zone, index));
        Reload(id);
    }

    private void MoveSelectedToZone(BarZone zone)
    {
        if (Selected() is { } sel) Move(sel.Item.Id, zone, int.MaxValue);
    }

    private void EarlierButton_Click(object sender, RoutedEventArgs e) => MoveSelectedWithinZone(-1);
    private void LaterButton_Click(object sender, RoutedEventArgs e) => MoveSelectedWithinZone(+1);
    private void ToLeftButton_Click(object sender, RoutedEventArgs e) => MoveSelectedToZone(BarZone.Left);
    private void ToCenterButton_Click(object sender, RoutedEventArgs e) => MoveSelectedToZone(BarZone.Center);
    private void ToRightButton_Click(object sender, RoutedEventArgs e) => MoveSelectedToZone(BarZone.Right);

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected() is { } sel) Move(sel.Item.Id, null, 0);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _s.Save(x => x.Layout = new LayoutSettings());
        Reload(null);
    }
}
