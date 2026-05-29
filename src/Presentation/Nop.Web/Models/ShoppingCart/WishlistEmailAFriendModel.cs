using Microsoft.AspNetCore.Mvc;

using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Validators.ShoppingCart;

namespace Nop.Web.Models.ShoppingCart
{
    
    public partial class WishlistEmailAFriendModel : BaseNopModel
    {
        
        [NopResourceDisplayName("Wishlist.EmailAFriend.FriendEmail")]
        public string FriendEmail { get; set; }

        
        [NopResourceDisplayName("Wishlist.EmailAFriend.YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        
        [NopResourceDisplayName("Wishlist.EmailAFriend.PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}