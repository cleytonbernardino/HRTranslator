using HHub.Shared.Utils;
using RTranslator.FIleIO;
using RTranslator.Models;

namespace RTranslator.ModelView;

internal class SelectProjectViewModel
{
    private readonly ExploreItemSelected _exploreItem;

    public SelectProjectViewModel(ExploreItemSelected exploreItem)
    {
        _exploreItem = exploreItem;
        Projects.AddRange(ExploreItemSerializer.Load());
    }

    public ObservableRangeCollection<ExploreItem> Projects { get; private set; } = [];

    public bool AddProject(string projectName)
    {
        var projectExist = Projects.Any(project => project.Name == projectName);
        if (projectExist)
        {
            return false;
        }
        Projects.Add(new() { Name = projectName });
        Save();
        return true;
    }

    public void RemoveProject(ExploreItem project)
    {
        Projects.Remove(project);
        Save();
    }

    public void SelectProject(ExploreItem project) => _exploreItem.Project = project;

    private void Save() => ExploreItemSerializer.Save(Projects);
}
