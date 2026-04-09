using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public interface IEmailAccountService
{
    Task<EmailAccount?> GetEmailAccountByIdAsync(int emailAccountId);
    Task<IList<EmailAccount>> GetAllEmailAccountsAsync();
    Task InsertEmailAccountAsync(EmailAccount emailAccount);
    Task UpdateEmailAccountAsync(EmailAccount emailAccount);
    Task DeleteEmailAccountAsync(EmailAccount emailAccount);
}
