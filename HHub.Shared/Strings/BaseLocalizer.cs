using Windows.ApplicationModel.Resources;

namespace HHub.Shared.Strings;

public abstract class BaseLocalizer
{
    public virtual ResourceLoader CSharpResourceLoader { get; }
    private readonly ResourceLoader _resourceLoaderError = new("HHub.Shared/ResourcesErros");

    /// <summary>
    /// Retrieve text from ResourceCSharp
    /// </summary>
    /// <param name="key">Key given to the item</param>
    /// <returns></returns>
    public string GetString(string key)
    {
        try
        {
            return CSharpResourceLoader.GetString(key);
        }
        catch
        {
            return "Ressource not found";
        }
    }

    /// <summary>
    /// Retrieve text from ResourceErrors
    /// </summary>
    /// <param name="key">Key given to the item</param>
    /// <returns></returns>
    public string GetErroString(string key) => _resourceLoaderError.GetString(key);
}
