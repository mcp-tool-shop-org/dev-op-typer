using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using DevOpTyper.ViewModels;

namespace DevOpTyper.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void TypingBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnKeyDown(e);
        }
    }
}
