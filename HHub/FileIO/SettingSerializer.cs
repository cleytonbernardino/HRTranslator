using HHub.Shared.Models;
using System.Reflection;
using System.Text;
using Windows.Storage;

namespace HHub.FIleIO;

internal class SettingSerializer()
{
    private static readonly string _SettingsDir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.ini");

    public static Settings Load()
    {
        try
        {
            string fileContent = File.ReadAllText(_SettingsDir);
            return ReadSettings(fileContent);
        }
        catch
        {
            return new Settings();
        }
    }

    public static void Save(Settings settingsInstance)
    {
        string settings = GetSettingsValue(settingsInstance);
        File.WriteAllText(_SettingsDir, settings);
    }

    private static IEnumerable<PropertyInfo> GetProperty()
    {
        var propertyInfo = typeof(Settings).GetProperties();

        foreach (PropertyInfo? prop in propertyInfo)
        {
            if (!prop.CanRead && !prop.CanWrite)
                continue;

            var type = prop.PropertyType;
            if (type.IsPrimitive == false && type != typeof(string) && type.IsEnum == false)
                continue;

            yield return prop;
        }
    }

    private static string GetSettingsValue(Settings settingsInstance)
    {
        StringBuilder settingsString = new(1000);
        foreach (var prop in GetProperty())
        {
            string propName = prop.Name;
            var value = prop.GetValue(settingsInstance);

            if (value is not null)
                settingsString.AppendLine($"{propName}= {value};\n");
        }
        return settingsString.ToString();
    }

    private static Settings ReadSettings(string settingsString)
    {
        Settings settings = new();
        foreach (var prop in GetProperty())
        {
            string propName = prop.Name;

            int fileProp = settingsString.IndexOf(propName);
            if (fileProp == -1)
                continue;

            int propEnd = settingsString.IndexOf(';', fileProp);

            string[] pLine = settingsString.Substring(fileProp, (propEnd - fileProp)).Split('=');
            string strValue = pLine[1].Trim();

            if (prop.PropertyType.IsPrimitive)
            {
                object? nValue = Convert.ChangeType(strValue, prop.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
                prop.SetValue(settings, nValue);
            }
            else if (prop.PropertyType.IsEnum)
            {
                if (Enum.TryParse(prop.PropertyType, strValue, out var enumValue))
                    prop.SetValue(settings, enumValue);
            }
            else
            {
                prop.SetValue(settings, strValue.Trim());
            }
        }
        return settings;
    }
}
