using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Services.Seo;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers;

public partial class CatalogController
{
    private async Task<CategoryModel> PrepareCategoryModelAsync(Category category, CatalogPagingFilteringModel command)
    {
        var seName = await category.GetSeNameAsync(0, urlRecordService);
        var pageSize = category.PageSize > 0 ? category.PageSize : catalogSettings.SearchPageProductsPerPage;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        var products = await productService.SearchProductsAsync(
            pageIndex: pageIndex, pageSize: pageSize,
            categoryIds: [category.Id],
            storeId: storeContext.CurrentStore.Id,
            visibleIndividuallyOnly: true,
            orderBy: (ProductSortingEnum)(command.OrderBy ?? 0));

        var model = new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            MetaKeywords = category.MetaKeywords,
            MetaDescription = category.MetaDescription,
            MetaTitle = category.MetaTitle,
            SeName = seName,
            PictureUrl = category.PictureId > 0
                ? await pictureService.GetPictureUrlAsync(category.PictureId, mediaSettings.CategoryThumbPictureSize)
                : null,
            PagingFilteringContext = { Page = pageIndex + 1, PageSize = pageSize, OrderBy = command.OrderBy },
        };

        // Breadcrumb
        model.DisplayCategoryBreadcrumb = catalogSettings.CategoryBreadcrumbEnabled;
        if (model.DisplayCategoryBreadcrumb)
        {
            var breadcrumb = new List<CategoryModel>();
            var current = category;
            while (current != null && !current.Deleted && current.Published)
            {
                var bcSeName = await current.GetSeNameAsync(0, urlRecordService);
                breadcrumb.Insert(0, new CategoryModel { Id = current.Id, Name = current.Name, SeName = bcSeName });
                current = current.ParentCategoryId > 0
                    ? await categoryService.GetCategoryByIdAsync(current.ParentCategoryId)
                    : null;
            }
            model.CategoryBreadcrumb = breadcrumb;
        }

        // Sub-categories
        var subCategories = await categoryService.GetAllCategoriesByParentCategoryIdAsync(category.Id);
        foreach (var sc in subCategories)
        {
            if (!sc.Published) continue;
            var scSeName = await sc.GetSeNameAsync(0, urlRecordService);
            model.SubCategories.Add(new CategoryModel.SubCategoryModel
            {
                Id = sc.Id,
                Name = sc.Name,
                SeName = scSeName,
                PictureUrl = sc.PictureId > 0
                    ? await pictureService.GetPictureUrlAsync(sc.PictureId, mediaSettings.CategoryThumbPictureSize)
                    : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.CategoryThumbPictureSize),
            });
        }

        // Products
        foreach (var p in products)
            model.Products.Add(await PrepareProductOverviewModelAsync(p));

        return model;
    }

    private async Task<ManufacturerModel> PrepareManufacturerModelAsync(Manufacturer manufacturer, CatalogPagingFilteringModel command)
    {
        var seName = await manufacturer.GetSeNameAsync(0, urlRecordService);
        var pageSize = manufacturer.PageSize > 0 ? manufacturer.PageSize : catalogSettings.SearchPageProductsPerPage;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        var products = await productService.SearchProductsAsync(
            pageIndex: pageIndex, pageSize: pageSize,
            manufacturerId: manufacturer.Id,
            storeId: storeContext.CurrentStore.Id,
            visibleIndividuallyOnly: true,
            orderBy: (ProductSortingEnum)(command.OrderBy ?? 0));

        var model = new ManufacturerModel
        {
            Id = manufacturer.Id,
            Name = manufacturer.Name,
            Description = manufacturer.Description,
            MetaKeywords = manufacturer.MetaKeywords,
            MetaDescription = manufacturer.MetaDescription,
            MetaTitle = manufacturer.MetaTitle,
            SeName = seName,
            PictureUrl = manufacturer.PictureId > 0
                ? await pictureService.GetPictureUrlAsync(manufacturer.PictureId, mediaSettings.ManufacturerThumbPictureSize)
                : null,
            PagingFilteringContext = { Page = pageIndex + 1, PageSize = pageSize, OrderBy = command.OrderBy },
        };

        foreach (var p in products)
            model.Products.Add(await PrepareProductOverviewModelAsync(p));

        return model;
    }

    private async Task<VendorModel> PrepareVendorModelAsync(Vendor vendor, CatalogPagingFilteringModel command)
    {
        var seName = await vendor.GetSeNameAsync(0, urlRecordService);
        var pageSize = vendor.PageSize > 0 ? vendor.PageSize : catalogSettings.SearchPageProductsPerPage;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        var products = await productService.SearchProductsAsync(
            pageIndex: pageIndex, pageSize: pageSize,
            vendorId: vendor.Id,
            storeId: storeContext.CurrentStore.Id,
            visibleIndividuallyOnly: true,
            orderBy: (ProductSortingEnum)(command.OrderBy ?? 0));

        var model = new VendorModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            Description = vendor.Description,
            MetaKeywords = vendor.MetaKeywords,
            MetaDescription = vendor.MetaDescription,
            MetaTitle = vendor.MetaTitle,
            SeName = seName,
            PictureUrl = vendor.PictureId > 0
                ? await pictureService.GetPictureUrlAsync(vendor.PictureId, mediaSettings.VendorThumbPictureSize)
                : null,
            PagingFilteringContext = { Page = pageIndex + 1, PageSize = pageSize, OrderBy = command.OrderBy },
        };

        foreach (var p in products)
            model.Products.Add(await PrepareProductOverviewModelAsync(p));

        return model;
    }

    private async Task<CategoryNavigationModel> PrepareCategoryNavigationModelAsync(int currentCategoryId)
    {
        var model = new CategoryNavigationModel { CurrentCategoryId = currentCategoryId };
        var rootCategories = await categoryService.GetAllCategoriesByParentCategoryIdAsync(0);
        foreach (var c in rootCategories)
        {
            if (!c.Published || !c.IncludeInTopMenu) continue;
            model.Categories.Add(await PrepareCategorySimpleModelAsync(c, currentCategoryId));
        }
        return model;
    }

    private async Task<TopMenuModel> PrepareTopMenuModelAsync()
    {
        var model = new TopMenuModel
        {
            BlogEnabled = true, // BlogSettings.Enabled — simplified
            ForumEnabled = true, // ForumSettings.ForumsEnabled — simplified
            DisplayHomePageMenuItem = true,
            DisplayProductSearchMenuItem = true,
            DisplayCustomerInfoMenuItem = true,
            DisplayBlogMenuItem = true,
            DisplayForumsMenuItem = true,
            DisplayContactUsMenuItem = true,
        };

        var rootCategories = await categoryService.GetAllCategoriesByParentCategoryIdAsync(0);
        foreach (var c in rootCategories)
        {
            if (!c.Published || !c.IncludeInTopMenu) continue;
            model.Categories.Add(await PrepareCategorySimpleModelAsync(c, 0));
        }
        return model;
    }

    private async Task<CategorySimpleModel> PrepareCategorySimpleModelAsync(Category category, int currentCategoryId)
    {
        var seName = await category.GetSeNameAsync(0, urlRecordService);
        var model = new CategorySimpleModel
        {
            Id = category.Id,
            Name = category.Name,
            SeName = seName,
            IncludeInTopMenu = category.IncludeInTopMenu,
        };

        if (catalogSettings.ShowCategoryProductNumber)
        {
            model.NumberOfProducts = await productService.GetNumberOfProductsInCategoryAsync(
                [category.Id], storeContext.CurrentStore.Id);
        }

        // Recursive sub-categories
        var subCategories = await categoryService.GetAllCategoriesByParentCategoryIdAsync(category.Id);
        foreach (var sc in subCategories)
        {
            if (!sc.Published || !sc.IncludeInTopMenu) continue;
            model.SubCategories.Add(await PrepareCategorySimpleModelAsync(sc, currentCategoryId));
        }

        return model;
    }

    private async Task<PopularProductTagsModel> PreparePopularProductTagsModelAsync(int maxTags)
    {
        var allTags = await productTagService.GetAllProductTagsAsync();
        var model = new PopularProductTagsModel { TotalTags = allTags.Count };

        var tagModels = new List<ProductTagModel>();
        foreach (var tag in allTags)
        {
            var count = await productTagService.GetProductCountAsync(tag.Id, storeContext.CurrentStore.Id);
            if (count <= 0) continue;
            tagModels.Add(new ProductTagModel
            {
                Id = tag.Id,
                Name = tag.Name,
                SeName = SeoExtensions.GetSeName(tag.Name, false, false),
                ProductCount = count,
            });
        }

        // Sort by count descending, take top N
        tagModels = tagModels.OrderByDescending(t => t.ProductCount).ToList();
        if (maxTags > 0 && maxTags < tagModels.Count)
            tagModels = tagModels.Take(maxTags).ToList();

        // Sort alphabetically for display
        model.Tags = tagModels.OrderBy(t => t.Name).ToList();
        return model;
    }

    private async Task<ProductsByTagModel> PrepareProductsByTagModelAsync(ProductTag productTag, CatalogPagingFilteringModel command)
    {
        var pageSize = catalogSettings.SearchPageProductsPerPage > 0 ? catalogSettings.SearchPageProductsPerPage : 6;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        var products = await productService.SearchProductsAsync(
            pageIndex: pageIndex, pageSize: pageSize,
            storeId: storeContext.CurrentStore.Id,
            productTagId: productTag.Id,
            visibleIndividuallyOnly: true,
            orderBy: (ProductSortingEnum)(command.OrderBy ?? 0));

        var model = new ProductsByTagModel
        {
            Id = productTag.Id,
            TagName = productTag.Name,
            TagSeName = SeoExtensions.GetSeName(productTag.Name, false, false),
            PagingFilteringContext = { Page = pageIndex + 1, PageSize = pageSize, OrderBy = command.OrderBy },
        };

        foreach (var p in products)
            model.Products.Add(await PrepareProductOverviewModelAsync(p));

        return model;
    }

    private async Task<SearchModel> PrepareSearchModelAsync(SearchModel model, CatalogPagingFilteringModel command)
    {
        model.PagingFilteringContext = command ?? new CatalogPagingFilteringModel();

        // Populate category/manufacturer/vendor dropdowns
        model.AvailableCategories.Insert(0, new SelectListItem
        {
            Text = await localizationService.GetResourceAsync("Common.All"),
            Value = "0",
        });
        var categories = await categoryService.GetAllCategoriesAsync(storeId: storeContext.CurrentStore.Id);
        foreach (var c in categories)
            model.AvailableCategories.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = c.Id == model.cid });

        model.AvailableManufacturers.Insert(0, new SelectListItem
        {
            Text = await localizationService.GetResourceAsync("Common.All"),
            Value = "0",
        });
        var manufacturers = await manufacturerService.GetAllManufacturersAsync(storeId: storeContext.CurrentStore.Id);
        foreach (var m in manufacturers)
            model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString(), Selected = m.Id == model.mid });

        model.asv = vendorSettings.AllowSearchByVendor;
        if (model.asv)
        {
            model.AvailableVendors.Insert(0, new SelectListItem
            {
                Text = await localizationService.GetResourceAsync("Common.All"),
                Value = "0",
            });
            var vendors = await vendorService.GetAllVendorsAsync();
            foreach (var v in vendors)
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString(), Selected = v.Id == model.vid });
        }

        // Execute search if query provided
        if (!string.IsNullOrWhiteSpace(model.q))
        {
            if (model.q.Length < catalogSettings.ProductSearchTermMinimumLength)
            {
                model.Warning = string.Format(
                    await localizationService.GetResourceAsync("Search.SearchTermMinimumLengthIsNCharacters"),
                    catalogSettings.ProductSearchTermMinimumLength);
            }
            else
            {
                var pageSize = catalogSettings.SearchPageProductsPerPage > 0 ? catalogSettings.SearchPageProductsPerPage : 6;
                var pageIndex = (command?.Page > 0 ? command.Page : 1) - 1;

                decimal.TryParse(model.pf, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var minPrice);
                decimal.TryParse(model.pt, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var maxPrice);

                var categoryIds = new List<int>();
                if (model.cid > 0)
                {
                    categoryIds.Add(model.cid);
                    if (model.isc)
                    {
                        var childCategories = await categoryService.GetAllCategoriesAsync(
                            storeId: storeContext.CurrentStore.Id);
                        categoryIds.AddRange(GetChildCategoryIds(childCategories.ToList(), model.cid));
                    }
                }

                var products = await productService.SearchProductsAsync(
                    pageIndex: pageIndex, pageSize: pageSize,
                    categoryIds: categoryIds.Count > 0 ? categoryIds : null,
                    manufacturerId: model.mid,
                    storeId: storeContext.CurrentStore.Id,
                    vendorId: model.vid,
                    visibleIndividuallyOnly: true,
                    priceMin: minPrice > 0 ? minPrice : null,
                    priceMax: maxPrice > 0 ? maxPrice : null,
                    keywords: model.q,
                    searchDescriptions: model.sid,
                    languageId: workContext.WorkingLanguage.Id,
                    orderBy: (ProductSortingEnum)(command?.OrderBy ?? 0));

                model.PagingFilteringContext.Page = pageIndex + 1;
                model.PagingFilteringContext.PageSize = pageSize;

                model.NoResults = products.Count == 0;

                foreach (var p in products)
                    model.Products.Add(await PrepareProductOverviewModelAsync(p));

                // Save search term
                if (!string.IsNullOrEmpty(model.q))
                {
                    var searchTerm = await searchTermService.GetSearchTermByKeywordAsync(model.q, storeContext.CurrentStore.Id);
                    if (searchTerm != null)
                    {
                        searchTerm.Count++;
                        await searchTermService.UpdateSearchTermAsync(searchTerm);
                    }
                    else
                    {
                        await searchTermService.InsertSearchTermAsync(new Nop.Core.Domain.Common.SearchTerm
                        {
                            Keyword = model.q,
                            StoreId = storeContext.CurrentStore.Id,
                            Count = 1,
                        });
                    }
                }
            }
        }

        return model;
    }

    private async Task<ProductOverviewModel> PrepareProductOverviewModelAsync(Product product)
    {
        var seName = await product.GetSeNameAsync(0, urlRecordService);

        // Picture
        var pictures = await pictureService.GetPicturesByProductIdAsync(product.Id, 1);
        var imageUrl = pictures.Count > 0
            ? await pictureService.GetPictureUrlAsync(pictures[0], mediaSettings.ProductThumbPictureSize)
            : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.ProductThumbPictureSize);

        // Price
        var finalPrice = await priceCalculationService.GetFinalPriceAsync(product, workContext.CurrentCustomer);
        var priceStr = await priceFormatter.FormatPriceAsync(finalPrice);
        string? oldPriceStr = null;
        if (product.OldPrice > 0 && product.OldPrice > finalPrice)
            oldPriceStr = await priceFormatter.FormatPriceAsync(product.OldPrice);

        return new ProductOverviewModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            SeName = seName,
            ImageUrl = imageUrl,
            Price = priceStr,
            OldPrice = oldPriceStr,
            MarkAsNew = product.MarkAsNew
                && (!product.MarkAsNewStartDateTimeUtc.HasValue || product.MarkAsNewStartDateTimeUtc.Value <= DateTime.UtcNow)
                && (!product.MarkAsNewEndDateTimeUtc.HasValue || product.MarkAsNewEndDateTimeUtc.Value >= DateTime.UtcNow),
        };
    }

    private static List<int> GetChildCategoryIds(List<Category> allCategories, int parentId)
    {
        var result = new List<int>();
        var children = allCategories.Where(c => c.ParentCategoryId == parentId && !c.Deleted && c.Published);
        foreach (var child in children)
        {
            result.Add(child.Id);
            result.AddRange(GetChildCategoryIds(allCategories, child.Id));
        }
        return result;
    }
}
