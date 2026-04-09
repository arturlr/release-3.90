using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Stores;

namespace Nop.Services.Messages;

public class MessageTemplateService(
    IRepository<MessageTemplate> messageTemplateRepository,
    IRepository<StoreMapping> storeMappingRepository,
    IStoreMappingService storeMappingService,
    ILanguageService languageService,
    ILocalizedEntityService localizedEntityService,
    CatalogSettings catalogSettings,
    IStaticCacheManager cacheManager,
    IEventPublisher eventPublisher) : IMessageTemplateService
{
    private const string AllKey = "Nop.messagetemplate.all-{0}";
    private const string ByNameKey = "Nop.messagetemplate.name-{0}-{1}";
    private const string PrefixKey = "Nop.messagetemplate.";

    public Task<MessageTemplate?> GetMessageTemplateByIdAsync(int messageTemplateId)
    {
        if (messageTemplateId == 0)
            return Task.FromResult<MessageTemplate?>(null);

        return Task.FromResult<MessageTemplate?>(messageTemplateRepository.GetById(messageTemplateId));
    }

    public async Task<MessageTemplate?> GetMessageTemplateByNameAsync(string messageTemplateName, int storeId)
    {
        if (string.IsNullOrWhiteSpace(messageTemplateName))
            return null;

        var key = new CacheKey(string.Format(ByNameKey, messageTemplateName, storeId), PrefixKey);
        return await cacheManager.GetAsync(key, () =>
        {
            var templates = messageTemplateRepository.Table
                .Where(t => t.Name == messageTemplateName)
                .OrderBy(t => t.Id)
                .ToList();

            if (storeId > 0)
                templates = templates.Where(t => storeMappingService.AuthorizeAsync(t, storeId).GetAwaiter().GetResult()).ToList();

            return Task.FromResult(templates.FirstOrDefault()!);
        });
    }

    public async Task<IList<MessageTemplate>> GetAllMessageTemplatesAsync(int storeId)
    {
        var key = new CacheKey(string.Format(AllKey, storeId), PrefixKey);
        return await cacheManager.GetAsync(key, () =>
        {
            var query = messageTemplateRepository.Table.OrderBy(t => t.Name).AsQueryable();

            if (storeId > 0 && !catalogSettings.IgnoreStoreLimitations)
            {
                query = from t in query
                        join sm in storeMappingRepository.Table
                            on new { c1 = t.Id, c2 = "MessageTemplate" }
                            equals new { c1 = sm.EntityId, c2 = sm.EntityName }
                            into t_sm
                        from sm in t_sm.DefaultIfEmpty()
                        where !t.LimitedToStores || storeId == sm.StoreId
                        select t;

                query = from t in query
                        group t by t.Id into tGroup
                        orderby tGroup.Key
                        select tGroup.First();

                query = query.OrderBy(t => t.Name);
            }

            return Task.FromResult<IList<MessageTemplate>>(query.ToList());
        }) ?? [];
    }

    public async Task InsertMessageTemplateAsync(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);
        messageTemplateRepository.Insert(messageTemplate);
        await cacheManager.RemoveByPrefixAsync(PrefixKey);
        await eventPublisher.EntityInsertedAsync(messageTemplate);
    }

    public async Task UpdateMessageTemplateAsync(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);
        messageTemplateRepository.Update(messageTemplate);
        await cacheManager.RemoveByPrefixAsync(PrefixKey);
        await eventPublisher.EntityUpdatedAsync(messageTemplate);
    }

    public async Task DeleteMessageTemplateAsync(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);
        messageTemplateRepository.Delete(messageTemplate);
        await cacheManager.RemoveByPrefixAsync(PrefixKey);
        await eventPublisher.EntityDeletedAsync(messageTemplate);
    }

    public async Task<MessageTemplate> CopyMessageTemplateAsync(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);

        var copy = new MessageTemplate
        {
            Name = messageTemplate.Name,
            BccEmailAddresses = messageTemplate.BccEmailAddresses,
            Subject = messageTemplate.Subject,
            Body = messageTemplate.Body,
            IsActive = messageTemplate.IsActive,
            AttachedDownloadId = messageTemplate.AttachedDownloadId,
            EmailAccountId = messageTemplate.EmailAccountId,
            LimitedToStores = messageTemplate.LimitedToStores,
            DelayBeforeSend = messageTemplate.DelayBeforeSend,
            DelayPeriodId = messageTemplate.DelayPeriodId
        };

        await InsertMessageTemplateAsync(copy);

        // copy localized values
        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);
        foreach (var lang in languages)
        {
            var bcc = await localizedEntityService.GetLocalizedValueAsync(lang.Id, messageTemplate.Id, "MessageTemplate", nameof(MessageTemplate.BccEmailAddresses));
            if (!string.IsNullOrEmpty(bcc))
                await localizedEntityService.SaveLocalizedValueAsync(copy, x => x.BccEmailAddresses, bcc, lang.Id);

            var subject = await localizedEntityService.GetLocalizedValueAsync(lang.Id, messageTemplate.Id, "MessageTemplate", nameof(MessageTemplate.Subject));
            if (!string.IsNullOrEmpty(subject))
                await localizedEntityService.SaveLocalizedValueAsync(copy, x => x.Subject, subject, lang.Id);

            var body = await localizedEntityService.GetLocalizedValueAsync(lang.Id, messageTemplate.Id, "MessageTemplate", nameof(MessageTemplate.Body));
            if (!string.IsNullOrEmpty(body))
                await localizedEntityService.SaveLocalizedValueAsync(copy, x => x.Body, body, lang.Id);

            var emailAccountId = await localizedEntityService.GetLocalizedValueAsync(lang.Id, messageTemplate.Id, "MessageTemplate", nameof(MessageTemplate.EmailAccountId));
            if (int.TryParse(emailAccountId, out var eaId) && eaId > 0)
                await localizedEntityService.SaveLocalizedValueAsync(copy, x => x.EmailAccountId, eaId, lang.Id);
        }

        // copy store mappings
        var storeIds = await storeMappingService.GetStoreIdsWithAccessAsync(messageTemplate);
        foreach (var id in storeIds)
            await storeMappingService.InsertStoreMappingAsync(messageTemplate, id);

        return copy;
    }
}
