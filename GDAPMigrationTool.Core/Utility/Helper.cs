using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Utility
{
    public class Helper
    {
        private static int progressValue = 1;
        private static string progressPrefix = string.Empty;
        /// <summary>
        ///  Get the extension in formated string
        /// </summary>
        /// <param name="exportImportType"></param>
        /// <returns></returns>
        public static string GetExtenstion(ExportImport exportImportType) => exportImportType.ToString().ToLower();


        public static bool UserConfirmation(string message, bool suppressExitMessage = true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\t");
            Console.WriteLine(message);
            Console.ResetColor();
            if (suppressExitMessage)
            {
                Console.WriteLine("press [y/Y] to continue or any other key to exit the operation.");
            }
            var option = Console.ReadLine();

            return option != null && option.Trim().ToLower() == "y";

        }

        public static void Spin()
        {
            if(!string.IsNullOrEmpty(progressPrefix))
            {
                Console.Write(progressPrefix + " " + progressValue++);
                progressPrefix = string.Empty;
            }
            else
            {
                Console.SetCursorPosition(Console.CursorLeft - progressValue.ToString().Length, Console.CursorTop);
                Console.Write(progressValue++);
            }   
        }

        public static void ResetSpin(string prefix)
        {
            progressValue = 1;
            progressPrefix = prefix;
        }
    }
}
