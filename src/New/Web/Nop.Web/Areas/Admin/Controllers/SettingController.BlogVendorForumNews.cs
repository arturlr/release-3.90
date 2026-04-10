using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Vendors;
using Nop.Web.Areas.Admin.Models.Settings;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SettingController
{
    #region Blog

    public async Task<IActionResult> Blog()
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<BlogSettings>(storeScope);
        var model = new BlogSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            Enabled = s.Enabled,
            PostsPageSize = s.PostsPageSize,
            AllowNotRegisteredUsersToLeaveComments = s.AllowNotRegisteredUsersToLeaveComments,
            NotifyAboutNewBlogComments = s.NotifyAboutNewBlogComments,
            NumberOfTags = s.NumberOfTags,
            ShowHeaderRssUrl = s.ShowHeaderRssUrl,
            BlogCommentsMustBeApproved = s.BlogCommentsMustBeApproved,
            ShowBlogCommentsPerStore = s.ShowBlogCommentsPerStore
        };
        if (storeScope > 0)
        {
            model.Enabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.Enabled, storeScope);
            model.PostsPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PostsPageSize, storeScope);
            model.AllowNotRegisteredUsersToLeaveComments_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowNotRegisteredUsersToLeaveComments, storeScope);
            model.NotifyAboutNewBlogComments_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NotifyAboutNewBlogComments, storeScope);
            model.NumberOfTags_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NumberOfTags, storeScope);
            model.ShowHeaderRssUrl_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowHeaderRssUrl, storeScope);
            model.BlogCommentsMustBeApproved_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.BlogCommentsMustBeApproved, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Blog(BlogSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<BlogSettings>(storeScope);
        s.Enabled = model.Enabled;
        s.PostsPageSize = model.PostsPageSize;
        s.AllowNotRegisteredUsersToLeaveComments = model.AllowNotRegisteredUsersToLeaveComments;
        s.NotifyAboutNewBlogComments = model.NotifyAboutNewBlogComments;
        s.NumberOfTags = model.NumberOfTags;
        s.ShowHeaderRssUrl = model.ShowHeaderRssUrl;
        s.BlogCommentsMustBeApproved = model.BlogCommentsMustBeApproved;
        s.ShowBlogCommentsPerStore = model.ShowBlogCommentsPerStore;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PostsPageSize, model.PostsPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowNotRegisteredUsersToLeaveComments, model.AllowNotRegisteredUsersToLeaveComments_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NotifyAboutNewBlogComments, model.NotifyAboutNewBlogComments_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NumberOfTags, model.NumberOfTags_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowHeaderRssUrl, model.ShowHeaderRssUrl_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.BlogCommentsMustBeApproved, model.BlogCommentsMustBeApproved_OverrideForStore, storeScope, false);
        await settingService.SaveSettingAsync(s, x => x.ShowBlogCommentsPerStore, clearCache: false);
        await settingService.ClearCacheAsync();

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Blog");
    }

    #endregion

    #region Vendor

    public async Task<IActionResult> Vendor()
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<VendorSettings>(storeScope);
        var model = new VendorSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            DefaultVendorPageSizeOptions = s.DefaultVendorPageSizeOptions,
            VendorsBlockItemsToDisplay = s.VendorsBlockItemsToDisplay,
            ShowVendorOnProductDetailsPage = s.ShowVendorOnProductDetailsPage,
            AllowCustomersToContactVendors = s.AllowCustomersToContactVendors,
            AllowCustomersToApplyForVendorAccount = s.AllowCustomersToApplyForVendorAccount,
            AllowSearchByVendor = s.AllowSearchByVendor,
            AllowVendorsToEditInfo = s.AllowVendorsToEditInfo,
            NotifyStoreOwnerAboutVendorInformationChange = s.NotifyStoreOwnerAboutVendorInformationChange,
            MaximumProductNumber = s.MaximumProductNumber,
            AllowVendorsToImportProducts = s.AllowVendorsToImportProducts
        };
        if (storeScope > 0)
        {
            model.DefaultVendorPageSizeOptions_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultVendorPageSizeOptions, storeScope);
            model.VendorsBlockItemsToDisplay_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.VendorsBlockItemsToDisplay, storeScope);
            model.ShowVendorOnProductDetailsPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowVendorOnProductDetailsPage, storeScope);
            model.AllowCustomersToContactVendors_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCustomersToContactVendors, storeScope);
            model.AllowCustomersToApplyForVendorAccount_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCustomersToApplyForVendorAccount, storeScope);
            model.AllowSearchByVendor_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowSearchByVendor, storeScope);
            model.AllowVendorsToEditInfo_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowVendorsToEditInfo, storeScope);
            model.NotifyStoreOwnerAboutVendorInformationChange_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NotifyStoreOwnerAboutVendorInformationChange, storeScope);
            model.MaximumProductNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MaximumProductNumber, storeScope);
            model.AllowVendorsToImportProducts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowVendorsToImportProducts, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor(VendorSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<VendorSettings>(storeScope);
        s.DefaultVendorPageSizeOptions = model.DefaultVendorPageSizeOptions;
        s.VendorsBlockItemsToDisplay = model.VendorsBlockItemsToDisplay;
        s.ShowVendorOnProductDetailsPage = model.ShowVendorOnProductDetailsPage;
        s.AllowCustomersToContactVendors = model.AllowCustomersToContactVendors;
        s.AllowCustomersToApplyForVendorAccount = model.AllowCustomersToApplyForVendorAccount;
        s.AllowSearchByVendor = model.AllowSearchByVendor;
        s.AllowVendorsToEditInfo = model.AllowVendorsToEditInfo;
        s.NotifyStoreOwnerAboutVendorInformationChange = model.NotifyStoreOwnerAboutVendorInformationChange;
        s.MaximumProductNumber = model.MaximumProductNumber;
        s.AllowVendorsToImportProducts = model.AllowVendorsToImportProducts;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultVendorPageSizeOptions, model.DefaultVendorPageSizeOptions_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.VendorsBlockItemsToDisplay, model.VendorsBlockItemsToDisplay_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowVendorOnProductDetailsPage, model.ShowVendorOnProductDetailsPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCustomersToContactVendors, model.AllowCustomersToContactVendors_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCustomersToApplyForVendorAccount, model.AllowCustomersToApplyForVendorAccount_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowSearchByVendor, model.AllowSearchByVendor_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowVendorsToEditInfo, model.AllowVendorsToEditInfo_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NotifyStoreOwnerAboutVendorInformationChange, model.NotifyStoreOwnerAboutVendorInformationChange_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MaximumProductNumber, model.MaximumProductNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowVendorsToImportProducts, model.AllowVendorsToImportProducts_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Vendor");
    }

    #endregion

    #region Forum

    public async Task<IActionResult> Forum()
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<ForumSettings>(storeScope);
        var model = new ForumSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            ForumsEnabled = s.ForumsEnabled,
            RelativeDateTimeFormattingEnabled = s.RelativeDateTimeFormattingEnabled,
            AllowCustomersToEditPosts = s.AllowCustomersToEditPosts,
            AllowCustomersToManageSubscriptions = s.AllowCustomersToManageSubscriptions,
            AllowGuestsToCreatePosts = s.AllowGuestsToCreatePosts,
            AllowGuestsToCreateTopics = s.AllowGuestsToCreateTopics,
            AllowCustomersToDeletePosts = s.AllowCustomersToDeletePosts,
            AllowPostVoting = s.AllowPostVoting,
            MaxVotesPerDay = s.MaxVotesPerDay,
            TopicSubjectMaxLength = s.TopicSubjectMaxLength,
            PostMaxLength = s.PostMaxLength,
            TopicsPageSize = s.TopicsPageSize,
            PostsPageSize = s.PostsPageSize,
            SearchResultsPageSize = s.SearchResultsPageSize,
            AllowPrivateMessages = s.AllowPrivateMessages,
            ShowAlertForPM = s.ShowAlertForPM,
            PrivateMessagesPageSize = s.PrivateMessagesPageSize,
            NotifyAboutPrivateMessages = s.NotifyAboutPrivateMessages,
            SignaturesEnabled = s.SignaturesEnabled,
            ForumSearchTermMinimumLength = s.ForumSearchTermMinimumLength
        };
        if (storeScope > 0)
        {
            model.ForumsEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ForumsEnabled, storeScope);
            model.RelativeDateTimeFormattingEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.RelativeDateTimeFormattingEnabled, storeScope);
            model.AllowCustomersToEditPosts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCustomersToEditPosts, storeScope);
            model.AllowCustomersToManageSubscriptions_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCustomersToManageSubscriptions, storeScope);
            model.AllowGuestsToCreatePosts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowGuestsToCreatePosts, storeScope);
            model.AllowGuestsToCreateTopics_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowGuestsToCreateTopics, storeScope);
            model.AllowCustomersToDeletePosts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCustomersToDeletePosts, storeScope);
            model.AllowPostVoting_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowPostVoting, storeScope);
            model.MaxVotesPerDay_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MaxVotesPerDay, storeScope);
            model.TopicSubjectMaxLength_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.TopicSubjectMaxLength, storeScope);
            model.PostMaxLength_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PostMaxLength, storeScope);
            model.TopicsPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.TopicsPageSize, storeScope);
            model.PostsPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PostsPageSize, storeScope);
            model.SearchResultsPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.SearchResultsPageSize, storeScope);
            model.AllowPrivateMessages_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowPrivateMessages, storeScope);
            model.ShowAlertForPM_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowAlertForPM, storeScope);
            model.PrivateMessagesPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PrivateMessagesPageSize, storeScope);
            model.NotifyAboutPrivateMessages_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NotifyAboutPrivateMessages, storeScope);
            model.SignaturesEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.SignaturesEnabled, storeScope);
            model.ForumSearchTermMinimumLength_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ForumSearchTermMinimumLength, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Forum(ForumSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<ForumSettings>(storeScope);
        s.ForumsEnabled = model.ForumsEnabled;
        s.RelativeDateTimeFormattingEnabled = model.RelativeDateTimeFormattingEnabled;
        s.AllowCustomersToEditPosts = model.AllowCustomersToEditPosts;
        s.AllowCustomersToManageSubscriptions = model.AllowCustomersToManageSubscriptions;
        s.AllowGuestsToCreatePosts = model.AllowGuestsToCreatePosts;
        s.AllowGuestsToCreateTopics = model.AllowGuestsToCreateTopics;
        s.AllowCustomersToDeletePosts = model.AllowCustomersToDeletePosts;
        s.AllowPostVoting = model.AllowPostVoting;
        s.MaxVotesPerDay = model.MaxVotesPerDay;
        s.TopicSubjectMaxLength = model.TopicSubjectMaxLength;
        s.PostMaxLength = model.PostMaxLength;
        s.TopicsPageSize = model.TopicsPageSize;
        s.PostsPageSize = model.PostsPageSize;
        s.SearchResultsPageSize = model.SearchResultsPageSize;
        s.AllowPrivateMessages = model.AllowPrivateMessages;
        s.ShowAlertForPM = model.ShowAlertForPM;
        s.PrivateMessagesPageSize = model.PrivateMessagesPageSize;
        s.NotifyAboutPrivateMessages = model.NotifyAboutPrivateMessages;
        s.SignaturesEnabled = model.SignaturesEnabled;
        s.ForumSearchTermMinimumLength = model.ForumSearchTermMinimumLength;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ForumsEnabled, model.ForumsEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.RelativeDateTimeFormattingEnabled, model.RelativeDateTimeFormattingEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCustomersToEditPosts, model.AllowCustomersToEditPosts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCustomersToManageSubscriptions, model.AllowCustomersToManageSubscriptions_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowGuestsToCreatePosts, model.AllowGuestsToCreatePosts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowGuestsToCreateTopics, model.AllowGuestsToCreateTopics_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCustomersToDeletePosts, model.AllowCustomersToDeletePosts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowPostVoting, model.AllowPostVoting_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MaxVotesPerDay, model.MaxVotesPerDay_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.TopicSubjectMaxLength, model.TopicSubjectMaxLength_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PostMaxLength, model.PostMaxLength_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.TopicsPageSize, model.TopicsPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PostsPageSize, model.PostsPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.SearchResultsPageSize, model.SearchResultsPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowPrivateMessages, model.AllowPrivateMessages_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowAlertForPM, model.ShowAlertForPM_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PrivateMessagesPageSize, model.PrivateMessagesPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NotifyAboutPrivateMessages, model.NotifyAboutPrivateMessages_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.SignaturesEnabled, model.SignaturesEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ForumSearchTermMinimumLength, model.ForumSearchTermMinimumLength_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Forum");
    }

    #endregion

    #region News

    public async Task<IActionResult> News()
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<NewsSettings>(storeScope);
        var model = new NewsSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            Enabled = s.Enabled,
            AllowNotRegisteredUsersToLeaveComments = s.AllowNotRegisteredUsersToLeaveComments,
            NotifyAboutNewNewsComments = s.NotifyAboutNewNewsComments,
            ShowNewsOnMainPage = s.ShowNewsOnMainPage,
            MainPageNewsCount = s.MainPageNewsCount,
            NewsArchivePageSize = s.NewsArchivePageSize,
            ShowHeaderRssUrl = s.ShowHeaderRssUrl,
            NewsCommentsMustBeApproved = s.NewsCommentsMustBeApproved,
            ShowNewsCommentsPerStore = s.ShowNewsCommentsPerStore
        };
        if (storeScope > 0)
        {
            model.Enabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.Enabled, storeScope);
            model.AllowNotRegisteredUsersToLeaveComments_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowNotRegisteredUsersToLeaveComments, storeScope);
            model.NotifyAboutNewNewsComments_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NotifyAboutNewNewsComments, storeScope);
            model.ShowNewsOnMainPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowNewsOnMainPage, storeScope);
            model.MainPageNewsCount_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MainPageNewsCount, storeScope);
            model.NewsArchivePageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NewsArchivePageSize, storeScope);
            model.ShowHeaderRssUrl_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowHeaderRssUrl, storeScope);
            model.NewsCommentsMustBeApproved_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NewsCommentsMustBeApproved, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> News(NewsSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<NewsSettings>(storeScope);
        s.Enabled = model.Enabled;
        s.AllowNotRegisteredUsersToLeaveComments = model.AllowNotRegisteredUsersToLeaveComments;
        s.NotifyAboutNewNewsComments = model.NotifyAboutNewNewsComments;
        s.ShowNewsOnMainPage = model.ShowNewsOnMainPage;
        s.MainPageNewsCount = model.MainPageNewsCount;
        s.NewsArchivePageSize = model.NewsArchivePageSize;
        s.ShowHeaderRssUrl = model.ShowHeaderRssUrl;
        s.NewsCommentsMustBeApproved = model.NewsCommentsMustBeApproved;
        s.ShowNewsCommentsPerStore = model.ShowNewsCommentsPerStore;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowNotRegisteredUsersToLeaveComments, model.AllowNotRegisteredUsersToLeaveComments_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NotifyAboutNewNewsComments, model.NotifyAboutNewNewsComments_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowNewsOnMainPage, model.ShowNewsOnMainPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MainPageNewsCount, model.MainPageNewsCount_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NewsArchivePageSize, model.NewsArchivePageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowHeaderRssUrl, model.ShowHeaderRssUrl_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NewsCommentsMustBeApproved, model.NewsCommentsMustBeApproved_OverrideForStore, storeScope, false);
        await settingService.SaveSettingAsync(s, x => x.ShowNewsCommentsPerStore, clearCache: false);
        await settingService.ClearCacheAsync();

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("News");
    }

    #endregion
}
