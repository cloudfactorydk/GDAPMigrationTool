namespace GDAPMigrationTool.Core.Utility
{
    public class Filehelper
    {
        public static void RenameFolder(string path)
        {
            var srcDir = Path.GetDirectoryName(path);
            // check the source folder have exist or not.
            if (Directory.Exists(srcDir))
            {
                // create Destination folder for moving
                var updateDestDir = CreateDirectory(srcDir);
                // move the files
                MoveFiles(srcDir, updateDestDir);
                // cleaning up the source folder
                //CleanUpFolder(srcDir);
            }
        }

        public static string CreateDirectory(string prefix)
        {
            //string dirName = prefix + " on " + DateTime.Now.ToString("ddd MM.dd.yyyy 'At' HH:mm tt");

            //Improved version of the above:
            var folderName = string.Format("{0:Bck yyy-MM-dd HH-mm-ss.fff}", DateTime.Now);
            var path = Path.Combine(prefix, folderName);
            Directory.CreateDirectory(path);
            return path;
        }


        public static void MoveFiles(string srcDir, string destDir)
        {
            if (Directory.Exists(srcDir) && Directory.Exists(destDir))
            {
                foreach (string file in Directory.EnumerateFiles(srcDir))
                {
                    if (!file.Contains("terminate") && !file.Contains("delete"))
                    {
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Move(file, destFile);
                    }
                }
            }
        }
    }
}
