using System.Collections.Generic;
using System.IO;
using Nop.Core;

namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// File permission helper
    /// </summary>
    public static class FilePermissionHelper
    {
        /// <summary>
        /// Check permissions
        /// </summary>
        public static bool CheckPermissions(string path, bool checkRead, bool checkWrite, bool checkModify, bool checkDelete)
        {
            // In ASP.NET Core on cross-platform, we simplify permission checking
            // by verifying the directory/file exists and is accessible
            try
            {
                if (Directory.Exists(path))
                {
                    if (checkWrite || checkModify)
                    {
                        var testFile = Path.Combine(path, "permission_test_" + System.Guid.NewGuid().ToString("N"));
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                    return true;
                }
                if (File.Exists(path))
                {
                    if (checkRead)
                    {
                        using (File.OpenRead(path)) { }
                    }
                    if (checkWrite)
                    {
                        using (File.OpenWrite(path)) { }
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of directories (physical paths) which require write permission
        /// </summary>
        public static IEnumerable<string> GetDirectoriesWrite()
        {
            string rootDir = CommonHelper.MapPath("~/");
            var dirsToCheck = new List<string>();
            dirsToCheck.Add(Path.Combine(rootDir, "App_Data"));
            dirsToCheck.Add(Path.Combine(rootDir, "bin"));
            dirsToCheck.Add(Path.Combine(rootDir, "content"));
            dirsToCheck.Add(Path.Combine(rootDir, "content", "images"));
            dirsToCheck.Add(Path.Combine(rootDir, "content", "images", "thumbs"));
            dirsToCheck.Add(Path.Combine(rootDir, "content", "images", "uploaded"));
            dirsToCheck.Add(Path.Combine(rootDir, "content", "files", "exportimport"));
            dirsToCheck.Add(Path.Combine(rootDir, "plugins"));
            dirsToCheck.Add(Path.Combine(rootDir, "plugins", "bin"));
            return dirsToCheck;
        }

        /// <summary>
        /// Gets a list of files (physical paths) which require write permission
        /// </summary>
        public static IEnumerable<string> GetFilesWrite()
        {
            string rootDir = CommonHelper.MapPath("~/");
            var filesToCheck = new List<string>();
            filesToCheck.Add(Path.Combine(rootDir, "App_Data", "InstalledPlugins.txt"));
            filesToCheck.Add(Path.Combine(rootDir, "App_Data", "Settings.txt"));
            return filesToCheck;
        }
    }
}
