using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Nop.Web.Framework.Mvc
{
    public class NopMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            var additionalValues = context.Attributes.OfType<IModelAttribute>().ToList();
            foreach (var additionalValue in additionalValues)
            {
                context.DisplayMetadata.AdditionalValues[additionalValue.Name] = additionalValue;
            }
        }
    }
}
