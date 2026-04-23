using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RTranslator.Components;

public sealed partial class ReplaceWindown : UserControl
{
    public event EventHandler? CloseRequested = null;
    public event EventHandler<(string replaceText, bool replaceAll)>? ReplaceRequest = null;


    public ReplaceWindown()
    {
        InitializeComponent();
    }

    private void ChangeReplaceVisibility()
    {
        bool isVisible = Txb_Replace.Visibility == Visibility.Visible;

        if (isVisible)
        {
            Skp_ReplaceBtn.Visibility = Visibility.Collapsed;
            Txb_Replace.Visibility = Visibility.Collapsed;
        } else
        {
            Skp_ReplaceBtn.Visibility = Visibility.Visible;
            Txb_Replace.Visibility = Visibility.Visible;
        }
    }

    private void Btn_Expander_Click(object sender, RoutedEventArgs e)
    {
        ChangeReplaceVisibility();
    }

    private void Btn_Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void Btn_Replace_Click(object sender, RoutedEventArgs e) => ReplaceRequest?.Invoke(this, (Txb_Replace.Text, false));

    private void Btn_ReplaceAll_Click(object sender, RoutedEventArgs e) => ReplaceRequest?.Invoke(this, (Txb_Replace.Text, true));
}
