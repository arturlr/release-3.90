using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Nop.Core.ComponentModel;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Sets the application up for the plugin referencing.
    /// Loads plugins using AssemblyLoadContext (replaces legacy BuildManager/shadow copy approach).
    /// </summary>
    public class PluginManager
    {
        #region Const

        private const string InstalledPluginsFilePath = "App_Data/InstalledPlugins.txt";
        private const string PluginsPath = "Plugins";
        private const string ShadowCopyPath = "Plugins/bin";

        #endregion

        #region Fields

        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();
        private static DirectoryInfo _shadowCopyFolder;
        private static bool _clearShadowDirectoryOnStartup;

        #endregion

        #region Properties

        /// <summary>
        /// Returns a collection of all referenced plugin assemblies that have been shadow copied
        /// </summary>
        public static IEnumerable<PluginDescriptor> ReferencedPlugins { get; set; }

        /// <summary>
        /// Returns a collection of all plugins which are not compatible with the current version
        /// </summary>
        public static IEnumerable<string> IncompatiblePlugins { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize plugin system
        /// </summary>
        /// <param name="clearShadowDirectory">Whether to clear the shadow copy directory on startup</param>
        public static void Initialize(bool clearShadowDirectory = false)
        {
            using (new WriteLockDisposable(Locker))
            {
                var pluginFolder = new DirectoryInfo(CommonHelper.MapPath(PluginsPath));
                _shadowCopyFolder = new DirectoryInfo(CommonHelper.MapPath(ShadowCopyPath));
                _clearShadowDirectoryOnStartup = clearShadowDirectory;

                var referencedPlugins = new List<PluginDescriptor>();
                var incompatiblePlugins = new List<string>();

                try
                {
                    var installedPluginSystemNames = PluginFileParser.ParseInstalledPluginsFile(GetInstalledPluginsFilePath());

                    Debug.WriteLine("Creating shadow copy folder and querying for dlls");
                    // Ensure folders are created
                    Directory.CreateDirectory(pluginFolder.FullName);
                    Directory.CreateDirectory(_shadowCopyFolder.FullName);

                    // Get list of all files in bin
                    var binFiles = _shadowCopyFolder.GetFiles("*", SearchOption.AllDirectories);
                    if (_clearShadowDirectoryOnStartup)
                    {
                        // Clear out shadow copied plugins
                        foreach (var f in binFiles)
                        {
                            Debug.WriteLine("Deleting " + f.Name);
                            try
                            {
                                File.Delete(f.FullName);
                            }
                            catch (Exception exc)
                            {
                                Debug.WriteLine("Error deleting file " + f.Name + ". Exception: " + exc);
                            }
                        }
                    }

                    // Load description files
                    foreach (var dfd in GetDescriptionFilesAndDescriptors(pluginFolder))
                    {
                        var descriptionFile = dfd.Key;
                        var pluginDescriptor = dfd.Value;

                        // Ensure that version of plugin is valid
                        if (!pluginDescriptor.SupportedVersions.Contains(NopVersion.CurrentVersion, StringComparer.InvariantCultureIgnoreCase))
                        {
                            incompatiblePlugins.Add(pluginDescriptor.SystemName);
                            continue;
                        }

                        // Some validation
                        if (string.IsNullOrWhiteSpace(pluginDescriptor.SystemName))
                            throw new Exception($"A plugin '{descriptionFile.FullName}' has no system name. Try assigning the plugin a unique name and recompiling.");
                        if (referencedPlugins.Contains(pluginDescriptor))
                            throw new Exception($"A plugin with '{pluginDescriptor.SystemName}' system name is already defined");

                        // Set 'Installed' property
                        pluginDescriptor.Installed = installedPluginSystemNames
                            .Any(x => x.Equals(pluginDescriptor.SystemName, StringComparison.InvariantCultureIgnoreCase));

                        try
                        {
                            if (descriptionFile.Directory == null)
                                throw new Exception($"Directory cannot be resolved for '{descriptionFile.Name}' description file");

                            // Get list of all DLLs in plugins (not in bin!)
                            var pluginFiles = descriptionFile.Directory.GetFiles("*.dll", SearchOption.AllDirectories)
                                .Where(x => !binFiles.Select(q => q.FullName).Contains(x.FullName))
                                .Where(x => IsPackagePluginFolder(x.Directory))
                                .ToList();

                            // Other plugin description info
                            var mainPluginFile = pluginFiles
                                .FirstOrDefault(x => x.Name.Equals(pluginDescriptor.PluginFileName, StringComparison.InvariantCultureIgnoreCase));
                            pluginDescriptor.OriginalAssemblyFile = mainPluginFile;

                            // Shadow copy and load main plugin file
                            pluginDescriptor.ReferencedAssembly = PerformFileDeploy(mainPluginFile);

                            // Load all other referenced assemblies now
                            foreach (var plugin in pluginFiles
                                .Where(x => !x.Name.Equals(mainPluginFile.Name, StringComparison.InvariantCultureIgnoreCase))
                                .Where(x => !IsAlreadyLoaded(x)))
                            {
                                PerformFileDeploy(plugin);
                            }

                            // Init plugin type (only one plugin per assembly is allowed)
                            foreach (var t in pluginDescriptor.ReferencedAssembly.GetTypes())
                            {
                                if (typeof(IPlugin).IsAssignableFrom(t))
                                {
                                    if (!t.IsInterface && t.IsClass && !t.IsAbstract)
                                    {
                                        pluginDescriptor.PluginType = t;
                                        break;
                                    }
                                }
                            }

                            referencedPlugins.Add(pluginDescriptor);
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            var msg = $"Plugin '{pluginDescriptor.FriendlyName}'. ";
                            foreach (var e in ex.LoaderExceptions)
                                msg += e.Message + Environment.NewLine;

                            throw new Exception(msg, ex);
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Plugin '{pluginDescriptor.FriendlyName}'. {ex.Message}";
                            throw new Exception(msg, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var msg = string.Empty;
                    for (var e = ex; e != null; e = e.InnerException)
                        msg += e.Message + Environment.NewLine;

                    throw new Exception(msg, ex);
                }

                ReferencedPlugins = referencedPlugins;
                IncompatiblePlugins = incompatiblePlugins;
            }
        }

        /// <summary>
        /// Mark plugin as installed
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public static void MarkPluginAsInstalled(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException(nameof(systemName));

            var filePath = CommonHelper.MapPath(InstalledPluginsFilePath);
            if (!File.Exists(filePath))
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (File.Create(filePath)) { }
            }

            var installedPluginSystemNames = PluginFileParser.ParseInstalledPluginsFile(GetInstalledPluginsFilePath());
            bool alreadyMarkedAsInstalled = installedPluginSystemNames
                .Any(x => x.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));

            if (!alreadyMarkedAsInstalled)
                installedPluginSystemNames.Add(systemName);

            PluginFileParser.SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
        }

        /// <summary>
        /// Mark plugin as uninstalled
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public static void MarkPluginAsUninstalled(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException(nameof(systemName));

            var filePath = CommonHelper.MapPath(InstalledPluginsFilePath);
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (File.Create(filePath)) { }
            }

            var installedPluginSystemNames = PluginFileParser.ParseInstalledPluginsFile(GetInstalledPluginsFilePath());
            bool alreadyMarkedAsInstalled = installedPluginSystemNames
                .Any(x => x.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));

            if (alreadyMarkedAsInstalled)
                installedPluginSystemNames.Remove(systemName);

            PluginFileParser.SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
        }

        /// <summary>
        /// Mark all plugins as uninstalled
        /// </summary>
        public static void MarkAllPluginsAsUninstalled()
        {
            var filePath = CommonHelper.MapPath(InstalledPluginsFilePath);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get description files
        /// </summary>
        /// <param name="pluginFolder">Plugin directory info</param>
        /// <returns>Original and parsed description files</returns>
        private static IEnumerable<KeyValuePair<FileInfo, PluginDescriptor>> GetDescriptionFilesAndDescriptors(DirectoryInfo pluginFolder)
        {
            if (pluginFolder == null)
                throw new ArgumentNullException(nameof(pluginFolder));

            var result = new List<KeyValuePair<FileInfo, PluginDescriptor>>();
            foreach (var descriptionFile in pluginFolder.GetFiles("Description.txt", SearchOption.AllDirectories))
            {
                if (!IsPackagePluginFolder(descriptionFile.Directory))
                    continue;

                var pluginDescriptor = PluginFileParser.ParsePluginDescriptionFile(descriptionFile.FullName);
                result.Add(new KeyValuePair<FileInfo, PluginDescriptor>(descriptionFile, pluginDescriptor));
            }

            // Sort by display order
            result.Sort((firstPair, nextPair) => firstPair.Value.DisplayOrder.CompareTo(nextPair.Value.DisplayOrder));
            return result;
        }

        /// <summary>
        /// Indicates whether assembly file is already loaded
        /// </summary>
        /// <param name="fileInfo">File info</param>
        /// <returns>Result</returns>
        private static bool IsAlreadyLoaded(FileInfo fileInfo)
        {
            try
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                if (fileNameWithoutExt == null)
                    throw new Exception($"Cannot get file extension for {fileInfo.Name}");

                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string assemblyName = a.FullName.Split(',').FirstOrDefault();
                    if (fileNameWithoutExt.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot validate whether an assembly is already loaded. " + exc);
            }
            return false;
        }

        /// <summary>
        /// Perform file deploy using shadow copy and AssemblyLoadContext
        /// </summary>
        /// <param name="plug">Plugin file info</param>
        /// <returns>Loaded Assembly</returns>
        private static Assembly PerformFileDeploy(FileInfo plug)
        {
            if (plug.Directory == null || plug.Directory.Parent == null)
                throw new InvalidOperationException(
                    $"The plugin directory for the {plug.Name} file exists in a folder outside of the allowed nopCommerce folder hierarchy");

            // Shadow copy the plugin to the bin folder
            var shadowCopyPlugFolder = Directory.CreateDirectory(_shadowCopyFolder.FullName);
            var shadowCopiedPlug = ShadowCopyFile(plug, shadowCopyPlugFolder);

            // Load the assembly using AssemblyLoadContext (replaces BuildManager.AddReferencedAssembly)
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(shadowCopiedPlug.FullName);

            Debug.WriteLine($"Loaded plugin assembly: '{assembly.FullName}'");
            return assembly;
        }

        /// <summary>
        /// Shadow copies a plugin file to the target folder
        /// </summary>
        /// <param name="plug">Source file</param>
        /// <param name="shadowCopyPlugFolder">Target shadow copy folder</param>
        /// <returns>Shadow copied file info</returns>
        private static FileInfo ShadowCopyFile(FileInfo plug, DirectoryInfo shadowCopyPlugFolder)
        {
            var shouldCopy = true;
            var shadowCopiedPlug = new FileInfo(Path.Combine(shadowCopyPlugFolder.FullName, plug.Name));

            // Check if a shadow copied file already exists and if it does, check if it's updated
            if (shadowCopiedPlug.Exists)
            {
                var areFilesIdentical = shadowCopiedPlug.LastWriteTimeUtc >= plug.LastWriteTimeUtc;
                if (areFilesIdentical)
                {
                    Debug.WriteLine($"Not copying; files appear identical: '{shadowCopiedPlug.Name}'");
                    shouldCopy = false;
                }
                else
                {
                    Debug.WriteLine($"New plugin found; Deleting the old file: '{shadowCopiedPlug.Name}'");
                    try
                    {
                        File.Delete(shadowCopiedPlug.FullName);
                    }
                    catch (IOException)
                    {
                        // If we can't delete, try to rename
                        var oldFile = shadowCopiedPlug.FullName + Guid.NewGuid().ToString("N") + ".old";
                        File.Move(shadowCopiedPlug.FullName, oldFile);
                    }
                }
            }

            if (shouldCopy)
            {
                try
                {
                    File.Copy(plug.FullName, shadowCopiedPlug.FullName, true);
                }
                catch (IOException)
                {
                    Debug.WriteLine(shadowCopiedPlug.FullName + " is locked, attempting to rename");
                    try
                    {
                        var oldFile = shadowCopiedPlug.FullName + Guid.NewGuid().ToString("N") + ".old";
                        File.Move(shadowCopiedPlug.FullName, oldFile);
                    }
                    catch (IOException exc)
                    {
                        throw new IOException(shadowCopiedPlug.FullName + " rename failed, cannot initialize plugin", exc);
                    }
                    // Retry the shadow copy
                    File.Copy(plug.FullName, shadowCopiedPlug.FullName, true);
                }
            }

            return shadowCopiedPlug;
        }

        /// <summary>
        /// Determines if the folder is a bin plugin folder for a package
        /// </summary>
        /// <param name="folder">Directory info</param>
        /// <returns>True if it's a package plugin folder</returns>
        private static bool IsPackagePluginFolder(DirectoryInfo folder)
        {
            if (folder == null) return false;
            if (folder.Parent == null) return false;
            if (!folder.Parent.Name.Equals("Plugins", StringComparison.InvariantCultureIgnoreCase)) return false;
            return true;
        }

        /// <summary>
        /// Gets the full path of InstalledPlugins.txt file
        /// </summary>
        /// <returns>Full file path</returns>
        private static string GetInstalledPluginsFilePath()
        {
            return CommonHelper.MapPath(InstalledPluginsFilePath);
        }

        #endregion
    }
}
