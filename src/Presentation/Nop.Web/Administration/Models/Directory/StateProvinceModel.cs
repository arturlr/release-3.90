using System.Collections.Generic;
using Nop.Admin.Validators.Directory;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Directory
{
    public partial class StateProvinceModel : BaseNopEntityModel, ILocalizedModel<StateProvinceLocalizedModel>
    {
        public StateProvinceModel()
        {
            Locales = new List<StateProvinceLocalizedModel>();
        }
        public int CountryId { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Countries.States.Fields.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Countries.States.Fields.Abbreviation")]

        public string Abbreviation { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Countries.States.Fields.Published")]
        public bool Published { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Countries.States.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<StateProvinceLocalizedModel> Locales { get; set; }
    }

    public partial class StateProvinceLocalizedModel : ILocalizedModelLocal
    {
        public int LanguageId { get; set; }
        
        [NopResourceDisplayName("Admin.Configuration.Countries.States.Fields.Name")]

        public string Name { get; set; }
    }
}