using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //We reordered our routes so the most used ones are on top. It can improve performance.

            //home page
            endpointRouteBuilder.MapControllerRoute("HomePage", "",
                new { controller = "Home", action = "Index" });

            //widgets
            endpointRouteBuilder.MapControllerRoute("WidgetsByZone", "widgetsbyzone/",
                new { controller = "Widget", action = "WidgetsByZone" });

            //login
            endpointRouteBuilder.MapControllerRoute("Login", "login/",
                new { controller = "Customer", action = "Login" });
            //register
            endpointRouteBuilder.MapControllerRoute("Register", "register/",
                new { controller = "Customer", action = "Register" });
            //logout
            endpointRouteBuilder.MapControllerRoute("Logout", "logout/",
                new { controller = "Customer", action = "Logout" });

            //shopping cart
            endpointRouteBuilder.MapControllerRoute("ShoppingCart", "cart/",
                new { controller = "ShoppingCart", action = "Cart" });
            //estimate shipping
            endpointRouteBuilder.MapControllerRoute("EstimateShipping", "cart/estimateshipping",
                new { controller = "ShoppingCart", action = "GetEstimateShipping" });
            //wishlist
            endpointRouteBuilder.MapControllerRoute("Wishlist", "wishlist/{customerGuid?}",
                new { controller = "ShoppingCart", action = "Wishlist" });

            //customer account links
            endpointRouteBuilder.MapControllerRoute("CustomerInfo", "customer/info",
                new { controller = "Customer", action = "Info" });
            endpointRouteBuilder.MapControllerRoute("CustomerAddresses", "customer/addresses",
                new { controller = "Customer", action = "Addresses" });
            endpointRouteBuilder.MapControllerRoute("CustomerOrders", "order/history",
                new { controller = "Order", action = "CustomerOrders" });

            //contact us
            endpointRouteBuilder.MapControllerRoute("ContactUs", "contactus",
                new { controller = "Common", action = "ContactUs" });
            //sitemap
            endpointRouteBuilder.MapControllerRoute("Sitemap", "sitemap",
                new { controller = "Common", action = "Sitemap" });

            //product search
            endpointRouteBuilder.MapControllerRoute("ProductSearch", "search/",
                new { controller = "Catalog", action = "Search" });
            endpointRouteBuilder.MapControllerRoute("ProductSearchAutoComplete", "catalog/searchtermautocomplete",
                new { controller = "Catalog", action = "SearchTermAutoComplete" });

            //change currency (AJAX link)
            endpointRouteBuilder.MapControllerRoute("ChangeCurrency", "changecurrency/{customercurrency:int}",
                new { controller = "Common", action = "SetCurrency" });
            //change language (AJAX link)
            endpointRouteBuilder.MapControllerRoute("ChangeLanguage", "changelanguage/{langid:int}",
                new { controller = "Common", action = "SetLanguage" });
            //change tax (AJAX link)
            endpointRouteBuilder.MapControllerRoute("ChangeTaxType", "changetaxtype/{customerTaxType:int}",
                new { controller = "Common", action = "SetTaxType" });

            //recently viewed products
            endpointRouteBuilder.MapControllerRoute("RecentlyViewedProducts", "recentlyviewedproducts/",
                new { controller = "Product", action = "RecentlyViewedProducts" });
            //new products
            endpointRouteBuilder.MapControllerRoute("NewProducts", "newproducts/",
                new { controller = "Product", action = "NewProducts" });

            //blog
            endpointRouteBuilder.MapControllerRoute("Blog", "blog",
                new { controller = "Blog", action = "List" });
            endpointRouteBuilder.MapControllerRoute("BlogByTag", "blog/tag/{tag}",
                new { controller = "Blog", action = "BlogByTag" });
            endpointRouteBuilder.MapControllerRoute("BlogByMonth", "blog/month/{month}",
                new { controller = "Blog", action = "BlogByMonth" });
            //blog RSS
            endpointRouteBuilder.MapControllerRoute("BlogRSS", "blog/rss/{languageId:int}",
                new { controller = "Blog", action = "ListRss" });

            //news RSS
            endpointRouteBuilder.MapControllerRoute("NewsRSS", "news/rss/{languageId:int}",
                new { controller = "News", action = "ListRss" });

            //news
            endpointRouteBuilder.MapControllerRoute("NewsArchive", "news",
                new { controller = "News", action = "List" });

            //forum
            endpointRouteBuilder.MapControllerRoute("Boards", "boards",
                new { controller = "Boards", action = "Index" });

            //compare products
            endpointRouteBuilder.MapControllerRoute("CompareProducts", "compareproducts/",
                new { controller = "Product", action = "CompareProducts" });

            //product tags
            endpointRouteBuilder.MapControllerRoute("ProductTagsAll", "producttag/all/",
                new { controller = "Catalog", action = "ProductTagsAll" });

            //manufacturers
            endpointRouteBuilder.MapControllerRoute("ManufacturerList", "manufacturer/all/",
                new { controller = "Catalog", action = "ManufacturerAll" });

            //vendors
            endpointRouteBuilder.MapControllerRoute("VendorList", "vendor/all/",
                new { controller = "Catalog", action = "VendorAll" });

            //add product to cart (without any attributes and target options)
            endpointRouteBuilder.MapControllerRoute("AddProductToCart-Catalog",
                "addproducttocart/catalog/{productId:int}/{shoppingCartTypeId:int}/{quantity:int}",
                new { controller = "ShoppingCart", action = "AddProductToCart_Catalog" });
            //add product to cart (with attributes and target options)
            endpointRouteBuilder.MapControllerRoute("AddProductToCart-Details",
                "addproducttocart/details/{productId:int}/{shoppingCartTypeId:int}",
                new { controller = "ShoppingCart", action = "AddProductToCart_Details" });

            //product tags
            endpointRouteBuilder.MapControllerRoute("ProductsByTag", "producttag/{productTagId:int}/{SeName?}",
                new { controller = "Catalog", action = "ProductsByTag" });

            //comparing products
            endpointRouteBuilder.MapControllerRoute("AddProductToCompare", "compareproducts/add/{productId:int}",
                new { controller = "Product", action = "AddProductToCompareList" });

            //product email a friend
            endpointRouteBuilder.MapControllerRoute("ProductEmailAFriend", "productemailafriend/{productId:int}",
                new { controller = "Product", action = "ProductEmailAFriend" });

            //reviews
            endpointRouteBuilder.MapControllerRoute("ProductReviews", "productreviews/{productId:int}",
                new { controller = "Product", action = "ProductReviews" });

            //back in stock notifications
            endpointRouteBuilder.MapControllerRoute("BackInStockSubscribePopup",
                "backinstocksubscribe/{productId:int}",
                new { controller = "BackInStockSubscription", action = "SubscribePopup" });
            endpointRouteBuilder.MapControllerRoute("BackInStockSubscribeSend",
                "backinstocksubscribesend/{productId:int}",
                new { controller = "BackInStockSubscription", action = "SubscribePopupPOST" });

            //downloads
            endpointRouteBuilder.MapControllerRoute("GetSampleDownload",
                "download/sample/{productid:int}",
                new { controller = "Download", action = "Sample" });

            //checkout pages
            endpointRouteBuilder.MapControllerRoute("Checkout", "checkout/",
                new { controller = "Checkout", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("CheckoutOnePage", "onepagecheckout/",
                new { controller = "Checkout", action = "OnePageCheckout" });
            endpointRouteBuilder.MapControllerRoute("CheckoutShippingAddress", "checkout/shippingaddress",
                new { controller = "Checkout", action = "ShippingAddress" });
            endpointRouteBuilder.MapControllerRoute("CheckoutSelectShippingAddress", "checkout/selectshippingaddress",
                new { controller = "Checkout", action = "SelectShippingAddress" });
            endpointRouteBuilder.MapControllerRoute("CheckoutBillingAddress", "checkout/billingaddress",
                new { controller = "Checkout", action = "BillingAddress" });
            endpointRouteBuilder.MapControllerRoute("CheckoutSelectBillingAddress", "checkout/selectbillingaddress",
                new { controller = "Checkout", action = "SelectBillingAddress" });
            endpointRouteBuilder.MapControllerRoute("CheckoutPaymentInfo", "checkout/paymentinfo",
                new { controller = "Checkout", action = "PaymentInfo" });
            endpointRouteBuilder.MapControllerRoute("CheckoutPaymentMethod", "checkout/paymentmethod",
                new { controller = "Checkout", action = "PaymentMethod" });
            endpointRouteBuilder.MapControllerRoute("CheckoutShippingMethod", "checkout/shippingmethod",
                new { controller = "Checkout", action = "ShippingMethod" });
            endpointRouteBuilder.MapControllerRoute("CheckoutConfirm", "checkout/confirm",
                new { controller = "Checkout", action = "Confirm" });
            endpointRouteBuilder.MapControllerRoute("CheckoutCompleted", "checkout/completed/{orderId:int?}",
                new { controller = "Checkout", action = "Completed" });

            //subscribe newsletters
            endpointRouteBuilder.MapControllerRoute("SubscribeNewsletter", "subscribenewsletter",
                new { controller = "Newsletter", action = "SubscribeNewsletter" });

            //email wishlist
            endpointRouteBuilder.MapControllerRoute("EmailWishlist", "emailwishlist",
                new { controller = "ShoppingCart", action = "EmailWishlist" });

            //login page for checkout as guest
            endpointRouteBuilder.MapControllerRoute("LoginCheckoutAsGuest", "login/checkoutasguest",
                new { controller = "Customer", action = "Login", checkoutAsGuest = true });

            //register result page
            endpointRouteBuilder.MapControllerRoute("RegisterResult", "registerresult/{resultId:int}",
                new { controller = "Customer", action = "RegisterResult" });

            //check username availability
            endpointRouteBuilder.MapControllerRoute("CheckUsernameAvailability", "customer/checkusernameavailability",
                new { controller = "Customer", action = "CheckUsernameAvailability" });

            //passwordrecovery
            endpointRouteBuilder.MapControllerRoute("PasswordRecovery", "passwordrecovery",
                new { controller = "Customer", action = "PasswordRecovery" });
            //password recovery confirmation
            endpointRouteBuilder.MapControllerRoute("PasswordRecoveryConfirm", "passwordrecovery/confirm",
                new { controller = "Customer", action = "PasswordRecoveryConfirm" });

            //topics
            endpointRouteBuilder.MapControllerRoute("TopicPopup",
                "t-popup/{SystemName}",
                new { controller = "Topic", action = "TopicDetailsPopup" });

            //blog comments
            endpointRouteBuilder.MapControllerRoute("BlogComment", "blog/comment/{customerid:int}",
                new { controller = "Blog", action = "BlogCommentsByCustomer" });

            //news comments
            endpointRouteBuilder.MapControllerRoute("NewsComment", "news/comment/{customerid:int}",
                new { controller = "News", action = "NewsCommentsByCustomer" });

            //product reviews
            endpointRouteBuilder.MapControllerRoute("CustomerProductReviews", "customer/productreviews",
                new { controller = "Product", action = "CustomerProductReviews" });

            //instock subscriptions
            endpointRouteBuilder.MapControllerRoute("CustomerBackInStockSubscriptions",
                "backinstocksubscriptions/manage",
                new { controller = "BackInStockSubscription", action = "CustomerSubscriptions" });
            endpointRouteBuilder.MapControllerRoute("CustomerBackInStockSubscriptionsPaged",
                "backinstocksubscriptions/manage/{page:int?}",
                new { controller = "BackInStockSubscription", action = "CustomerSubscriptions" });

            //customer profile page
            endpointRouteBuilder.MapControllerRoute("CustomerProfile", "profile/{id:int}",
                new { controller = "Profile", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("CustomerProfilePaged",
                "profile/{id:int}/page/{page:int}",
                new { controller = "Profile", action = "Index" });

            //orders
            endpointRouteBuilder.MapControllerRoute("OrderDetails", "orderdetails/{orderId:int}",
                new { controller = "Order", action = "Details" });
            endpointRouteBuilder.MapControllerRoute("ShipmentDetails", "orderdetails/shipment/{shipmentId:int}",
                new { controller = "Order", action = "ShipmentDetails" });
            endpointRouteBuilder.MapControllerRoute("ReturnRequest", "returnrequest/{orderId:int}",
                new { controller = "ReturnRequest", action = "ReturnRequest" });
            endpointRouteBuilder.MapControllerRoute("ReOrder", "reorder/{orderId:int}",
                new { controller = "Order", action = "ReOrder" });
            endpointRouteBuilder.MapControllerRoute("GetOrderPdfInvoice", "orderdetails/pdf/{orderId:int}",
                new { controller = "Order", action = "GetPdfInvoice" });
            endpointRouteBuilder.MapControllerRoute("PrintOrderDetails", "orderdetails/print/{orderId:int}",
                new { controller = "Order", action = "PrintOrderDetails" });

            //order downloads
            endpointRouteBuilder.MapControllerRoute("GetDownload", "download/getdownload/{orderItemId:guid}/{agree?}",
                new { controller = "Download", action = "GetDownload" });
            endpointRouteBuilder.MapControllerRoute("GetLicense", "download/getlicense/{orderItemId:guid}",
                new { controller = "Download", action = "GetLicense" });
            endpointRouteBuilder.MapControllerRoute("DownloadUserAgreement", "customer/useragreement/{orderItemId:guid}",
                new { controller = "Customer", action = "UserAgreement" });
            endpointRouteBuilder.MapControllerRoute("GetOrderNoteFile", "download/ordernotefile/{ordernoteid:int}",
                new { controller = "Download", action = "GetOrderNoteFile" });

            //contact vendor
            endpointRouteBuilder.MapControllerRoute("ContactVendor", "contactvendor/{vendorId:int}",
                new { controller = "Common", action = "ContactVendor" });

            //apply for vendor account
            endpointRouteBuilder.MapControllerRoute("ApplyVendorAccount", "vendor/apply",
                new { controller = "Vendor", action = "ApplyVendor" });

            //vendor info
            endpointRouteBuilder.MapControllerRoute("CustomerVendorInfo", "customer/vendorinfo",
                new { controller = "Vendor", action = "Info" });

            //customer change password
            endpointRouteBuilder.MapControllerRoute("CustomerChangePassword", "customer/changepassword",
                new { controller = "Customer", action = "ChangePassword" });

            //customer avatar
            endpointRouteBuilder.MapControllerRoute("CustomerAvatar", "customer/avatar",
                new { controller = "Customer", action = "Avatar" });

            //customer account activation
            endpointRouteBuilder.MapControllerRoute("AccountActivation", "customer/activation",
                new { controller = "Customer", action = "AccountActivation" });

            //customer forum subscriptions
            endpointRouteBuilder.MapControllerRoute("CustomerForumSubscriptions",
                "boards/forumsubscriptions",
                new { controller = "Boards", action = "CustomerForumSubscriptions" });
            endpointRouteBuilder.MapControllerRoute("CustomerForumSubscriptionsPaged",
                "boards/forumsubscriptions/{page:int?}",
                new { controller = "Boards", action = "CustomerForumSubscriptions" });

            //customer address edit
            endpointRouteBuilder.MapControllerRoute("CustomerAddressEdit",
                "customer/addressedit/{addressId:int}",
                new { controller = "Customer", action = "AddressEdit" });
            endpointRouteBuilder.MapControllerRoute("CustomerAddressAdd",
                "customer/addressadd",
                new { controller = "Customer", action = "AddressAdd" });

            //customer downloadable products
            endpointRouteBuilder.MapControllerRoute("CustomerDownloadableProducts",
                "customer/downloadableproducts",
                new { controller = "Customer", action = "DownloadableProducts" });

            //customer return requests
            endpointRouteBuilder.MapControllerRoute("CustomerReturnRequests",
                "returnrequest/history",
                new { controller = "ReturnRequest", action = "CustomerReturnRequests" });

            //customer reward points
            endpointRouteBuilder.MapControllerRoute("CustomerRewardPoints",
                "reward/history",
                new { controller = "Order", action = "CustomerRewardPoints" });

            //private messages
            endpointRouteBuilder.MapControllerRoute("PrivateMessages", "privatemessages",
                new { controller = "PrivateMessages", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("PrivateMessagesPaged",
                "privatemessages/page/{page:int?}",
                new { controller = "PrivateMessages", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("PrivateMessagesSent", "privatemessages/sent",
                new { controller = "PrivateMessages", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("PrivateMessagesSentPaged",
                "privatemessages/sent/page/{page:int?}",
                new { controller = "PrivateMessages", action = "Index" });
            endpointRouteBuilder.MapControllerRoute("SendPM", "sendpm/{toCustomerId:int}",
                new { controller = "PrivateMessages", action = "SendPM" });
            endpointRouteBuilder.MapControllerRoute("SendPMReply", "sendpm/{toCustomerId:int}/{replyToMessageId:int}",
                new { controller = "PrivateMessages", action = "SendPM" });
            endpointRouteBuilder.MapControllerRoute("ViewPM", "viewpm/{privateMessageId:int}",
                new { controller = "PrivateMessages", action = "ViewPM" });
            endpointRouteBuilder.MapControllerRoute("DeletePM", "deletepm/{privateMessageId:int}",
                new { controller = "PrivateMessages", action = "DeletePM" });

            //activate newsletters
            endpointRouteBuilder.MapControllerRoute("NewsletterActivation",
                "newsletter/subscriptionactivation/{token:guid}/{active}",
                new { controller = "Newsletter", action = "SubscriptionActivation" });

            //robots.txt
            endpointRouteBuilder.MapControllerRoute("robots.txt", "robots.txt",
                new { controller = "Common", action = "RobotsTextFile" });

            //sitemap (XML)
            endpointRouteBuilder.MapControllerRoute("sitemap.xml", "sitemap.xml",
                new { controller = "Common", action = "SitemapXml" });
            endpointRouteBuilder.MapControllerRoute("sitemap-indexed.xml", "sitemap-{Id:int}.xml",
                new { controller = "Common", action = "SitemapXml" });

            //store closed
            endpointRouteBuilder.MapControllerRoute("StoreClosed", "storeclosed",
                new { controller = "Common", action = "StoreClosed" });

            //install
            endpointRouteBuilder.MapControllerRoute("Installation", "install",
                new { controller = "Install", action = "Index" });

            //page not found
            endpointRouteBuilder.MapControllerRoute("PageNotFound", "page-not-found",
                new { controller = "Common", action = "PageNotFound" });
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
