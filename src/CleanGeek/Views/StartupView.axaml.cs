using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CleanGeek.Views;

public partial class StartupView : UserControl
{
    public StartupView() => AvaloniaXamlLoader.Load(this);
}
