using Microsoft.AspNetCore.Mvc;
using Nop.Core.Infrastructure;
using Nop.Web.Factories;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Core.Domain.Catalog;
using System.Linq;

namespace Nop.Web.Components
{
    public class LogoViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICommonModelFactory>().PrepareLogoModel();
            return View("~/Views/Common/Logo.cshtml", model);
        }
    }

    public class HeaderLinksViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICommonModelFactory>().PrepareHeaderLinksModel();
            return View("~/Views/Common/HeaderLinks.cshtml", model);
        }
    }

    public class FooterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICommonModelFactory>().PrepareFooterModel();
            return View("~/Views/Common/Footer.cshtml", model);
        }
    }

    public class TopMenuViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICatalogModelFactory>().PrepareTopMenuModel();
            return View("~/Views/Catalog/TopMenu.cshtml", model);
        }
    }

    public class SearchBoxViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICatalogModelFactory>().PrepareSearchBoxModel();
            return View("~/Views/Catalog/SearchBox.cshtml", model);
        }
    }

    public class HomepageCategoriesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = EngineContext.Current.Resolve<ICatalogModelFactory>().PrepareHomepageCategoryModels();
            if (!model.Any())
                return Content("");
            return View("~/Views/Catalog/HomepageCategories.cshtml", model);
        }
    }

    public class HomepageProductsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int? productThumbPictureSize = null)
        {
            var productService = EngineContext.Current.Resolve<IProductService>();
            var productModelFactory = EngineContext.Current.Resolve<IProductModelFactory>();
            var aclService = EngineContext.Current.Resolve<IAclService>();
            var storeMappingService = EngineContext.Current.Resolve<IStoreMappingService>();

            var products = productService.GetAllProductsDisplayedOnHomePage();
            products = products.Where(p => aclService.Authorize(p) && storeMappingService.Authorize(p)).ToList();
            products = products.Where(p => p.IsAvailable()).ToList();
            if (!products.Any())
                return Content("");
            var model = productModelFactory.PrepareProductOverviewModels(products, true, true, productThumbPictureSize).ToList();
            return View("~/Views/Product/HomepageProducts.cshtml", model);
        }
    }
}
