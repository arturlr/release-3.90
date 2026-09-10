using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
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
        /// <param name="path">Path</param>
        /// <param name="checkRead">Check read</param>
        /// <param name="checkWrite">Check write</param>
        /// <param name="checkModify">Check modify</param>
        /// <param name="checkDelete">Check delete</param>
        /// <returns>Result</returns>
        /// <remarks>
        /// <para>
        /// <b>Windows only.</b> The whole body is Windows ACL evaluation:
        /// <see cref="WindowsIdentity"/>, <see cref="FileSystemAccessRule"/>,
        /// <see cref="FileSystemRights"/> and <see cref="DirectoryInfo"/>'s
        /// <c>GetAccessControl()</c> are all annotated
        /// <c>[SupportedOSPlatform("windows")]</c> in the net10.0 reference assemblies. POSIX
        /// permissions have no equivalent model, and nopCommerce 3.90 had no other
        /// implementation to port.
        /// </para>
        /// <para>
        /// The <c>[SupportedOSPlatform("windows")]</c> attribute below is <b>purely
        /// declarative and changes no behaviour</b> — it states the constraint the code has
        /// always had, which is what the migration already records as an accepted Windows-first
        /// trade-off (design section 7, and the <c>.csproj</c> note on why the TFM stays the
        /// OS-agnostic <c>net10.0</c>). It resolves 49 <c>CA1416</c> platform-compatibility
        /// warnings in this file that were previously suppressed by nothing at all.
        /// It is deliberately placed on this method and NOT on the class:
        /// <see cref="GetDirectoriesWrite"/> and <see cref="GetFilesWrite"/> are plain path
        /// arithmetic and are platform-neutral, so annotating the class would export a
        /// constraint they do not have.
        /// </para>
        /// <para>
        /// <b>Consequence for tasks 7.3 and 8.3:</b> the four call sites —
        /// <c>Nop.Web/Controllers/InstallController.cs</c> and
        /// <c>Nop.Web/Administration/Controllers/CommonController.cs</c> — will now each raise
        /// a <c>CA1416</c> warning of their own, because they are reachable on all platforms.
        /// That is the attribute working as intended: it surfaces at compile time what would
        /// otherwise be a <c>PlatformNotSupportedException</c> from
        /// <c>WindowsIdentity.GetCurrent()</c> at runtime on Linux. Those tasks should either
        /// guard the call with <c>OperatingSystem.IsWindows()</c> (which the analyser
        /// recognises, and which would also make the install/system-info page work off
        /// Windows) or annotate their own member.
        /// </para>
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public static bool CheckPermissions(string path, bool checkRead, bool checkWrite, bool checkModify, bool checkDelete)
        {
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            bool flag5 = false;
            bool flag6 = false;
            bool flag7 = false;
            bool flag8 = false;
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            AuthorizationRuleCollection rules;
            try
            {
                //.NET Framework had the static System.IO.Directory.GetAccessControl(string).
                //On .NET the ACL API moved to extension methods on DirectoryInfo/FileInfo
                //(System.IO.FileSystemAclExtensions), so the static overload no longer exists.
                //Verified against the net10.0 reference assemblies: GetAccessControl() and
                //GetAccessControl(AccessControlSections) both hang off DirectoryInfo.
                //(runtime deferral 34, folded in by task 6.4 so gate 6.6 is a clean zero)
                rules = new DirectoryInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
            }
            catch
            {
                return true;
            }
            try
            {
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (!current.User.Equals(rule.IdentityReference))
                    {
                        continue;
                    }
                    if (AccessControlType.Deny.Equals(rule.AccessControlType))
                    {
                        if ((FileSystemRights.Delete & rule.FileSystemRights) == FileSystemRights.Delete)
                            flag4 = true;
                        if ((FileSystemRights.Modify & rule.FileSystemRights) == FileSystemRights.Modify)
                            flag3 = true;

                        if ((FileSystemRights.Read & rule.FileSystemRights) == FileSystemRights.Read)
                            flag = true;

                        if ((FileSystemRights.Write & rule.FileSystemRights) == FileSystemRights.Write)
                            flag2 = true;

                        continue;
                    }
                    if (AccessControlType.Allow.Equals(rule.AccessControlType))
                    {
                        if ((FileSystemRights.Delete & rule.FileSystemRights) == FileSystemRights.Delete)
                        {
                            flag8 = true;
                        }
                        if ((FileSystemRights.Modify & rule.FileSystemRights) == FileSystemRights.Modify)
                        {
                            flag7 = true;
                        }
                        if ((FileSystemRights.Read & rule.FileSystemRights) == FileSystemRights.Read)
                        {
                            flag5 = true;
                        }
                        if ((FileSystemRights.Write & rule.FileSystemRights) == FileSystemRights.Write)
                        {
                            flag6 = true;
                        }
                    }
                }
                foreach (IdentityReference reference in current.Groups)
                {
                    foreach (FileSystemAccessRule rule2 in rules)
                    {
                        if (!reference.Equals(rule2.IdentityReference))
                        {
                            continue;
                        }
                        if (AccessControlType.Deny.Equals(rule2.AccessControlType))
                        {
                            if ((FileSystemRights.Delete & rule2.FileSystemRights) == FileSystemRights.Delete)
                                flag4 = true;
                            if ((FileSystemRights.Modify & rule2.FileSystemRights) == FileSystemRights.Modify)
                                flag3 = true;
                            if ((FileSystemRights.Read & rule2.FileSystemRights) == FileSystemRights.Read)
                                flag = true;
                            if ((FileSystemRights.Write & rule2.FileSystemRights) == FileSystemRights.Write)
                                flag2 = true;
                            continue;
                        }
                        if (AccessControlType.Allow.Equals(rule2.AccessControlType))
                        {
                            if ((FileSystemRights.Delete & rule2.FileSystemRights) == FileSystemRights.Delete)
                                flag8 = true;
                            if ((FileSystemRights.Modify & rule2.FileSystemRights) == FileSystemRights.Modify)
                                flag7 = true;
                            if ((FileSystemRights.Read & rule2.FileSystemRights) == FileSystemRights.Read)
                                flag5 = true;
                            if ((FileSystemRights.Write & rule2.FileSystemRights) == FileSystemRights.Write)
                                flag6 = true;
                        }
                    }
                }
                bool flag9 = !flag4 && flag8;
                bool flag10 = !flag3 && flag7;
                bool flag11 = !flag && flag5;
                bool flag12 = !flag2 && flag6;
                bool flag13 = true;
                if (checkRead)
                {
                    //flag13 = flag13 && flag11;
                    flag13 = flag11;
                }
                if (checkWrite)
                {
                    flag13 = flag13 && flag12;
                }
                if (checkModify)
                {
                    flag13 = flag13 && flag10;
                }
                if (checkDelete)
                {
                    flag13 = flag13 && flag9;
                }
                return flag13;
            }
            catch (IOException)
            {
            }
            return false;
        }

        /// <summary>
        /// Gets a list of directories (physical paths) which require write permission
        /// </summary>
        /// <returns>Result</returns>
        public static IEnumerable<string> GetDirectoriesWrite()
        {
            string rootDir = CommonHelper.MapPath("~/");
            var dirsToCheck = new List<string>();
            //dirsToCheck.Add(rootDir);
            dirsToCheck.Add(Path.Combine(rootDir, "App_Data"));
            dirsToCheck.Add(Path.Combine(rootDir, "Administration\\db_backups"));
            dirsToCheck.Add(Path.Combine(rootDir, "bin"));
            dirsToCheck.Add(Path.Combine(rootDir, "content"));
            dirsToCheck.Add(Path.Combine(rootDir, "content\\images"));
            dirsToCheck.Add(Path.Combine(rootDir, "content\\images\\thumbs"));
            dirsToCheck.Add(Path.Combine(rootDir, "content\\images\\uploaded"));
            dirsToCheck.Add(Path.Combine(rootDir, "content\\files\\exportimport"));
            dirsToCheck.Add(Path.Combine(rootDir, "plugins"));
            dirsToCheck.Add(Path.Combine(rootDir, "plugins\\bin"));
            return dirsToCheck;
        }

        /// <summary>
        /// Gets a list of files (physical paths) which require write permission
        /// </summary>
        /// <returns>Result</returns>
        /// <remarks>
        /// <para>
        /// <b>Task 7.5 removed the <c>Global.asax</c> entry</b> (runtime deferral 18.5). The file
        /// was deleted by task 7.2 — <c>System.Web</c>'s <c>HttpApplication</c> hosting model has
        /// no ASP.NET Core counterpart and its members moved to <c>Program.cs</c> — so asking for
        /// write permission on it was asking about a path that cannot exist. It was harmless
        /// rather than broken, because <see cref="CheckPermissions"/> wraps the ACL read in
        /// <c>try { … } catch { return true; }</c> and therefore reported a non-existent path as
        /// "permission OK", but it made the installer and the admin System Info page assert
        /// something meaningless.
        /// </para>
        /// <para>
        /// <c>web.config</c> is <b>deliberately KEPT</b>. Task 7.4 reduced it to the IIS/ANCM
        /// hosting shim but did not remove it, and the ASP.NET Core Module rewrites it on publish,
        /// so write access to it is still a legitimate thing to verify on an IIS deployment. Note
        /// the name is now correct on a case-sensitive filesystem for the first time: this list
        /// always said lowercase <c>web.config</c> while 3.90's file on disk was
        /// <c>Web.config</c>, so the check silently missed the real file on Linux until task 7.4
        /// renamed it to all-lowercase.
        /// </para>
        /// <para>
        /// The remaining <c>\\</c> path separators are pre-existing 3.90 behaviour and are left
        /// alone; they are coupled to the Windows-first posture recorded on
        /// <see cref="CheckPermissions"/>, and changing them would alter what the installer and
        /// System Info page report.
        /// </para>
        /// </remarks>
        public static IEnumerable<string> GetFilesWrite()
        {
            string rootDir = CommonHelper.MapPath("~/");
            var filesToCheck = new List<string>();
            filesToCheck.Add(Path.Combine(rootDir, "web.config"));
            filesToCheck.Add(Path.Combine(rootDir, "App_Data\\InstalledPlugins.txt"));
            filesToCheck.Add(Path.Combine(rootDir, "App_Data\\Settings.txt"));
            return filesToCheck;
        }
    }
}
