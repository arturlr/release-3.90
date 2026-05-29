using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Provides information about types in the current web application. 
    /// Optionally this class can look at all assemblies in the bin folder.
    /// </summary>
    public class WebAppTypeFinder : AppDomainTypeFinder
    {
        #region Fields

        private bool _ensureBinFolderAssembliesLoaded = true;
        private bool _binFolderAssembliesLoaded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether assemblies in the bin folder of the web application should be specifically 
        /// checked for being loaded on application load. This is needed in situations where plugins need 
        /// to be loaded in the AppDomain after the application has been reloaded.
        /// </summary>
        public bool EnsureBinFolderAssembliesLoaded
        {
            get { return _ensureBinFolderAssembliesLoaded; }
            set { _ensureBinFolderAssembliesLoaded = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a physical disk path of the bin directory
        /// </summary>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\"</returns>
        public virtual string GetBinDirectory()
        {
            // In ASP.NET Core, the application base directory is the content root
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// Gets the assemblies related to the current implementation.
        /// </summary>
        /// <returns>A list of assemblies</returns>
        public override IList<Assembly> GetAssemblies()
        {
            if (EnsureBinFolderAssembliesLoaded && !_binFolderAssembliesLoaded)
            {
                _binFolderAssembliesLoaded = true;
                string binPath = GetBinDirectory();
                LoadMatchingAssemblies(binPath);
            }

            return base.GetAssemblies();
        }

        #endregion
    }
}
