using System.Reflection;
using PartnerLed;

namespace CloudFactoryGdapMigrator.Utility;

public static class GetCurrentPathHelper
{
    public static string GetCurrentPath()
    {
        string? path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        if (string.IsNullOrWhiteSpace(path))
            path = Path.GetDirectoryName(typeof(AppSettingsConfiguration).Assembly.Location);
        if (string.IsNullOrWhiteSpace(path))
            path = Directory.GetCurrentDirectory();

        return path;
    }
}