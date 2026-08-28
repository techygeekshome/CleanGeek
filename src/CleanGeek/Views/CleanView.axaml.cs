using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CleanGeek.Views;

public partial class CleanView : UserControl
{
    public CleanView() => AvaloniaXamlLoader.Load(this);
}
