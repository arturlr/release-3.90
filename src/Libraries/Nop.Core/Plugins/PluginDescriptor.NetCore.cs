using System.Collections.Generic;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Plugin descriptor for .NET 8
    /// </summary>
    public class PluginDescriptor
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string AssemblyName { get; set; }
        public List<string> SupportedVersions { get; set; }
        public List<string> Dependencies { get; set; }
        public string Category { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsInstalled { get; set; }
        
        // Runtime properties (not in JSON)
        public System.Reflection.Assembly PluginAssembly { get; set; }
        public System.Type PluginType { get; set; }
        public object LoadContext { get; set; } // PluginLoadContext
    }
}
