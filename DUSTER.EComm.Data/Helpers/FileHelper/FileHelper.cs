namespace DUSTER.EComm.Data.Helpers.FileHelper
{
    public static class FileHelper
    {
        public static char DirectorySeparatorChar = Path.DirectorySeparatorChar;

        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string LocalFileStoragePath(string FolderName = "CommonFiles")
        {
            //string path = $"{Directory.GetCurrentDirectory()}{DirectorySeparatorChar}Files{DirectorySeparatorChar}{FolderName}{DirectorySeparatorChar}";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Files", FolderName);
            return path.Substring(path.IndexOf("Files"));
        }

        public static string ImportFilePath()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Files","Import","Upload");
            return path.Substring(path.IndexOf("Files"));
        }
        public static string ImportErrorFilePath()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Files", "Import", "Error");
            return path.Substring(path.IndexOf("Files"));
        }
    }
}
