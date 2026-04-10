using System.Net;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Forums;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Profile;

namespace Nop.Web.Controllers;

public class ProfileController(
    ICustomerService customerService,
    IGenericAttributeService genericAttributeService,
    IForumService forumService,
    IPictureService pictureService,
    ICountryService countryService,
    IDateTimeHelper dateTimeHelper,
    ILocalizationService localizationService,
    CustomerSettings customerSettings,
    ForumSettings forumSettings,
    MediaSettings mediaSettings) : BasePublicController
{
    public async Task<IActionResult> Index(int? id, int? page)
    {
        if (!customerSettings.AllowViewingProfiles)
            return RedirectToAction("Index", "Home");

        var customer = id.HasValue ? await customerService.GetCustomerByIdAsync(id.Value) : null;
        if (customer == null || customer.Deleted)
            return RedirectToAction("Index", "Home");

        // check guest
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        if (guestRole != null && roleIds.Contains(guestRole.Id) && roleIds.Length == 1)
            return RedirectToAction("Index", "Home");

        var pagingPosts = page.HasValue;
        var postsPage = page ?? 0;

        var name = customer.Email ?? string.Empty;
        var title = string.Format(
            await localizationService.GetResourceAsync("Profile.ProfileOf"), name);

        var model = new ProfileIndexModel
        {
            CustomerProfileId = customer.Id,
            ProfileTitle = title,
            PostsPage = postsPage,
            PagingPosts = pagingPosts,
            ForumsEnabled = forumSettings.ForumsEnabled,
            Info = await PrepareInfoModelAsync(customer),
            Posts = forumSettings.ForumsEnabled
                ? await PreparePostsModelAsync(customer, postsPage)
                : new ProfilePostsModel()
        };

        return View(model);
    }

    private async Task<ProfileInfoModel> PrepareInfoModelAsync(Customer customer)
    {
        // avatar
        var avatarUrl = string.Empty;
        if (customerSettings.AllowCustomersToUploadAvatars)
        {
            var avatarPictureId = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.AvatarPictureId, genericAttributeService);
            avatarUrl = await pictureService.GetPictureUrlAsync(
                avatarPictureId, mediaSettings.AvatarPictureSize,
                customerSettings.DefaultAvatarEnabled,
                defaultPictureType: PictureType.Avatar);
        }

        // location
        var locationEnabled = false;
        var location = string.Empty;
        if (customerSettings.ShowCustomersLocation)
        {
            var countryId = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.CountryId, genericAttributeService);
            var country = await countryService.GetCountryByIdAsync(countryId);
            if (country != null)
            {
                locationEnabled = true;
                location = country.Name ?? string.Empty;
            }
        }

        // private messages
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var isGuest = guestRole != null && roleIds.Contains(guestRole.Id) && roleIds.Length == 1;
        var pmEnabled = forumSettings.AllowPrivateMessages && !isGuest;

        // total forum posts
        var totalPostsEnabled = false;
        var totalPosts = 0;
        if (forumSettings.ForumsEnabled && forumSettings.ShowCustomersPostCount)
        {
            totalPostsEnabled = true;
            totalPosts = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.ForumPostCount, genericAttributeService);
        }

        // join date
        var joinDateEnabled = false;
        var joinDate = string.Empty;
        if (customerSettings.ShowCustomersJoinDate)
        {
            joinDateEnabled = true;
            joinDate = dateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc).ToString("f");
        }

        // date of birth
        var dateOfBirthEnabled = false;
        var dateOfBirth = string.Empty;
        if (customerSettings.DateOfBirthEnabled)
        {
            var dob = await customer.GetAttributeAsync<DateTime?>(
                SystemCustomerAttributeNames.DateOfBirth, genericAttributeService);
            if (dob.HasValue)
            {
                dateOfBirthEnabled = true;
                dateOfBirth = dob.Value.ToString("D");
            }
        }

        return new ProfileInfoModel
        {
            CustomerProfileId = customer.Id,
            AvatarUrl = avatarUrl,
            LocationEnabled = locationEnabled,
            Location = location,
            PMEnabled = pmEnabled,
            TotalPostsEnabled = totalPostsEnabled,
            TotalPosts = totalPosts.ToString(),
            JoinDateEnabled = joinDateEnabled,
            JoinDate = joinDate,
            DateOfBirthEnabled = dateOfBirthEnabled,
            DateOfBirth = dateOfBirth
        };
    }

    private async Task<ProfilePostsModel> PreparePostsModelAsync(Customer customer, int page)
    {
        var pageIndex = page > 0 ? page - 1 : 0;
        var pageSize = forumSettings.LatestCustomerPostsPageSize;

        var posts = await forumService.GetAllPostsAsync(
            customerId: customer.Id, pageIndex: pageIndex, pageSize: pageSize);

        var postModels = new List<PostModel>();
        foreach (var forumPost in posts)
        {
            var topic = await forumService.GetTopicByIdAsync(forumPost.TopicId);
            var topicTitle = topic?.Subject ?? string.Empty;
            var topicSlug = topic != null
                ? SeoExtensions.GetSeName(topic.Subject, false, false)
                : string.Empty;

            var posted = forumSettings.RelativeDateTimeFormattingEnabled
                ? FormatRelativeDate(forumPost.CreatedOnUtc)
                : dateTimeHelper.ConvertToUserTime(forumPost.CreatedOnUtc, DateTimeKind.Utc).ToString("f");

            postModels.Add(new PostModel
            {
                ForumTopicId = forumPost.TopicId,
                ForumTopicTitle = topicTitle,
                ForumTopicSlug = topicSlug,
                ForumPostText = WebUtility.HtmlEncode(forumPost.Text ?? string.Empty)
                    .Replace("\n", "<br />"),
                Posted = posted
            });
        }

        return new ProfilePostsModel
        {
            Posts = postModels,
            PageIndex = posts.PageIndex,
            PageSize = posts.PageSize,
            TotalCount = posts.TotalCount,
            CustomerProfileId = customer.Id
        };
    }

    private static string FormatRelativeDate(DateTime utcDate)
    {
        var diff = DateTime.UtcNow - utcDate;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} days ago";
        return utcDate.ToString("f");
    }
}
