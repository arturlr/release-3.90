using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Messages;
using Nop.Services.Events;

namespace Nop.Services.Messages;

public class EmailAccountService(
    IRepository<EmailAccount> emailAccountRepository,
    IEventPublisher eventPublisher) : IEmailAccountService
{
    public Task<EmailAccount?> GetEmailAccountByIdAsync(int emailAccountId)
    {
        if (emailAccountId == 0)
            return Task.FromResult<EmailAccount?>(null);

        return Task.FromResult<EmailAccount?>(emailAccountRepository.GetById(emailAccountId));
    }

    public Task<IList<EmailAccount>> GetAllEmailAccountsAsync()
    {
        var accounts = emailAccountRepository.Table.OrderBy(ea => ea.Id).ToList();
        return Task.FromResult<IList<EmailAccount>>(accounts);
    }

    public async Task InsertEmailAccountAsync(EmailAccount emailAccount)
    {
        ArgumentNullException.ThrowIfNull(emailAccount);

        emailAccount.Email = (emailAccount.Email ?? string.Empty).Trim();
        emailAccount.DisplayName = (emailAccount.DisplayName ?? string.Empty).Trim();
        emailAccount.Host = (emailAccount.Host ?? string.Empty).Trim();
        emailAccount.Username = (emailAccount.Username ?? string.Empty).Trim();
        emailAccount.Password = (emailAccount.Password ?? string.Empty).Trim();

        emailAccountRepository.Insert(emailAccount);
        await eventPublisher.EntityInsertedAsync(emailAccount);
    }

    public async Task UpdateEmailAccountAsync(EmailAccount emailAccount)
    {
        ArgumentNullException.ThrowIfNull(emailAccount);

        emailAccount.Email = (emailAccount.Email ?? string.Empty).Trim();
        emailAccount.DisplayName = (emailAccount.DisplayName ?? string.Empty).Trim();
        emailAccount.Host = (emailAccount.Host ?? string.Empty).Trim();
        emailAccount.Username = (emailAccount.Username ?? string.Empty).Trim();
        emailAccount.Password = (emailAccount.Password ?? string.Empty).Trim();

        emailAccountRepository.Update(emailAccount);
        await eventPublisher.EntityUpdatedAsync(emailAccount);
    }

    public async Task DeleteEmailAccountAsync(EmailAccount emailAccount)
    {
        ArgumentNullException.ThrowIfNull(emailAccount);

        if (emailAccountRepository.Table.Count() == 1)
            throw new NopException("You cannot delete this email account. At least one account is required.");

        emailAccountRepository.Delete(emailAccount);
        await eventPublisher.EntityDeletedAsync(emailAccount);
    }
}
