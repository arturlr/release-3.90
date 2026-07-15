using System.Collections.Generic;
using System.IO;
using Nop.Core;

namespace Nop.Web.Framework.Security
{
    public static class FilePermissionHelper
    {
        public static bool CheckPermissions(string path, bool checkRead, bool checkWrite, bool checkModify, bool checkDelete)
        {
            // In ASP.NET Core cross-platform, file permission checks are simplified
            // We attempt basic file system access to verify permissions
            try
            {
                if (checkRead)
                {
                    if (Directory.Exists(path))
                        Directory.GetFiles(path);
                    else if (File.Exists(path))
                        File.OpenRead(path).Dispose();
                }
                if (checkWrite)
                {
                    if (Directory.Exists(path))
                    {
                        var testFile = Path.Combine(path, "__nop_permission_test.tmp");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

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
