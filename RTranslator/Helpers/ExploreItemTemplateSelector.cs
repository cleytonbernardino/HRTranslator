using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RTranslator.Models;

namespace RTranslator.Helpers;

public partial class ExplorerItemTemplateSelector : DataTemplateSelector
{
#pragma warning disable CS8618 // Non-nullable field
    public DataTemplate FolderTemplate { get; set; }

    public DataTemplate FileTemplate { get; set; }
#pragma warning restore CS8618 // Non-nullable field

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var project = (ExploreItem)item;

        return project.IsFile ? FileTemplate : FolderTemplate ;
    }
}
