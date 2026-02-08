using Microsoft.UI.Xaml;

namespace DevOpTyper;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
    }
}
