using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public class ChangePasswordRequest
{
    public ChangePasswordRequest(string email, bool validateRequest,
        PasswordFormat newPasswordFormat, string newPassword, string oldPassword = "")
    {
        Email = email;
        ValidateRequest = validateRequest;
        NewPasswordFormat = newPasswordFormat;
        NewPassword = newPassword;
        OldPassword = oldPassword;
    }

    public string Email { get; set; }
    public bool ValidateRequest { get; set; }
    public PasswordFormat NewPasswordFormat { get; set; }
    public string NewPassword { get; set; }
    public string OldPassword { get; set; }
}
