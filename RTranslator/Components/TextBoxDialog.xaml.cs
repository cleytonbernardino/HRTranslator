using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RTranslator.Components;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TextBoxDialog : ContentDialog
{
    public TextBoxDialog()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string PlaceHolder
    {
        get => (string)GetValue(PlaceHolderProperty);
        set => SetValue(PlaceHolderProperty, value);
    }

    public string? GetResultText()
    {
        return TbxProjectName.Text;
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBoxDialog), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty PlaceHolderProperty =
        DependencyProperty.Register(nameof(PlaceHolder), typeof(string), typeof(TextBoxDialog), new PropertyMetadata(default(string)));
}
