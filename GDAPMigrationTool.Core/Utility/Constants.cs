using CloudFactoryGdapMigrator.Utility;

namespace PartnerLed.Utility
{
    internal class Constants
    {
        public static readonly string InputFolderPath = GetCurrentPathHelper.GetCurrentPath() + "/GDAPBulkMigration/operations";

        public static readonly string OutputFolderPath = GetCurrentPathHelper.GetCurrentPath() + "/GDAPBulkMigration/downloads";
        
        public static readonly string LogFolderPath = GetCurrentPathHelper.GetCurrentPath() + "/Logs";

        public const string BasepathVariable = "BasepathForOperations";

    }
}
