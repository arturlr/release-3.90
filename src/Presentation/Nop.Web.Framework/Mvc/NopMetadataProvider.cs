using System;
using System.Linq;
using Nop.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;


namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// This MetadataProvider adds custom attributes (implementing IModelAttribute) to the model's metadata
    /// so that it can be retrieved later.
    /// In ASP.NET Core, this is implemented as an IDisplayMetadataProvider.
    /// </summary>
    public class NopMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.PropertyAttributes != null)
            {
                var additionalValues = context.PropertyAttributes.OfType<IModelAttribute>().ToList();
                foreach (var additionalValue in additionalValues)
                {
                    context.DisplayMetadata.AdditionalValues[additionalValue.Name] = additionalValue;
                }
            }
        }
    }
}
