using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using TopBar.App.ViewModels;
using TopBar.Core.Services;
using Windows.UI;

namespace TopBar.App.Controls;

/// <summary>Virtual desktops: a dot per desktop, the active one is an accent-colored pill. Changes are animated (180 ms, CubicEase).</summary>
public sealed partial class DesktopIndicator : UserControl
{
    private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(180));

    private sealed class Item
    {
        public required Border Hit { get; init; }
        public required Border Dot { get; init; }
        public required SolidColorBrush Brush { get; init; }
        public bool Active { get; set; }
        public bool Hover { get; set; }
    }

    private readonly DesktopsViewModel _vm;
    private readonly ISettingsService _settings;
    private readonly List<Item> _items = new();
    private int _dotSize;

    public DesktopIndicator(DesktopsViewModel vm, ISettingsService settings)
    {
        InitializeComponent();
        _vm = vm;
        _settings = settings;

        PointerWheelChanged += OnWheel;
        ActualThemeChanged += (_, _) => Sync(animate: false, force: true);
        Loaded += (_, _) => { _vm.Updated += OnUpdated; Sync(false, true); };
        Unloaded += (_, _) => _vm.Updated -= OnUpdated;
    }

    private void OnUpdated() => Sync(animate: true, force: false);

    private Color ActiveColor => ((SolidColorBrush)Resources["ActiveBrush"]).Color;
    private Color InactiveColor => ((SolidColorBrush)Resources["InactiveBrush"]).Color;
    private Color HoverColor => ((SolidColorBrush)Resources["HoverBrush"]).Color;

    private void Sync(bool animate, bool force)
    {
        Visibility = _vm.IsVisible && _vm.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        var list = _vm.Items;
        var current = _vm.CurrentIndex;
        var size = _settings.Current.Desktops.DotSize;

        if (force || _items.Count != list.Count || _dotSize != size)
        {
            Rebuild(list, current, size);
            return;
        }

        var storyboard = new Storyboard();
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            ToolTipService.SetToolTip(item.Hit, DesktopsViewModel.DisplayName(list[i]));
            var active = i == current;
            if (active != item.Active) Apply(item, active, animate ? storyboard : null);
        }
        if (storyboard.Children.Count > 0) storyboard.Begin();
    }

    private void Rebuild(IReadOnlyList<DesktopInfo> list, int current, int size)
    {
        _dotSize = size;
        Row.Children.Clear();
        _items.Clear();

        for (var i = 0; i < list.Count; i++)
        {
            var index = i;
            var brush = new SolidColorBrush(InactiveColor);
            var dot = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2.0),
                Background = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var hit = new Border
            {
                MinWidth = 16,
                Padding = new Thickness(3, 0, 3, 0),
                Background = new SolidColorBrush(Colors.Transparent), // invisible but hit-testable
                Child = dot,
            };
            ToolTipService.SetToolTip(hit, DesktopsViewModel.DisplayName(list[i]));

            var item = new Item { Hit = hit, Dot = dot, Brush = brush };
            hit.Tapped += (_, e) => { _vm.SwitchTo(index); e.Handled = true; };
            hit.PointerEntered += (_, _) => { item.Hover = true; if (!item.Active) brush.Color = HoverColor; };
            hit.PointerExited += (_, _) => { item.Hover = false; if (!item.Active) brush.Color = InactiveColor; };

            _items.Add(item);
            Row.Children.Add(hit);
            Apply(item, i == current, null);
        }
    }

    /// <summary>Sets the final state; with a storyboard the change is animated from the old to the new value.</summary>
    private void Apply(Item item, bool active, Storyboard? storyboard)
    {
        var oldWidth = item.Dot.Width;
        var oldColor = item.Brush.Color;
        var newWidth = active ? _dotSize * 3 : _dotSize;
        var newColor = active ? ActiveColor : item.Hover ? HoverColor : InactiveColor;

        item.Active = active;
        item.Dot.Width = newWidth;
        item.Brush.Color = newColor;

        if (storyboard is null) return;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var width = new DoubleAnimation
        {
            From = oldWidth, To = newWidth, Duration = AnimationDuration, EasingFunction = ease,
            EnableDependentAnimation = true, FillBehavior = FillBehavior.Stop,
        };
        Storyboard.SetTarget(width, item.Dot);
        Storyboard.SetTargetProperty(width, "Width");

        var color = new ColorAnimation
        {
            From = oldColor, To = newColor, Duration = AnimationDuration, EasingFunction = ease,
            FillBehavior = FillBehavior.Stop,
        };
        Storyboard.SetTarget(color, item.Brush);
        Storyboard.SetTargetProperty(color, "Color");

        storyboard.Children.Add(width);
        storyboard.Children.Add(color);
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (delta == 0) return;
        _vm.Scroll(delta > 0 ? -1 : 1); // wheel up = previous, wheel down = next
        e.Handled = true;
    }
}
