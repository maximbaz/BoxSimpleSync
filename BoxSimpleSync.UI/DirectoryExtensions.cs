using System.IO;

namespace BoxSimpleSync.UI
{
    public static class DirectoryExtensions
    {
        public static string GetLastWriteTimestamp(string path) {
            return Directory.GetLastWriteTime(path).ToString("yyyyMMddHHmmssffff");
        }
    }
}