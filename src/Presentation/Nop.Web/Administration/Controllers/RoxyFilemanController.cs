using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;

namespace Nop.Admin.Controllers
{
    public class RoxyFilemanController : BaseAdminController
    {
        private readonly IPermissionService _permissionService;

        public RoxyFilemanController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public virtual IActionResult Index()
        {
            // TODO: Implement Roxy file manager for ASP.NET Core
            throw new NotImplementedException("RoxyFileman not yet migrated to ASP.NET Core");
        }
    }
}
