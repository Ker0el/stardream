using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace StarrySky.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
    }
}
