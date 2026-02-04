using System.IO;

namespace Script.CommonLib
{
    public static class IOHelper
    {
        public static void EnsureDirectory(string path)
        {
            var directoryName = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(directoryName))
                return;
            
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
        }
    }
}
