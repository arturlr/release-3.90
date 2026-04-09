using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public interface IMessageTemplateService
{
    Task<MessageTemplate?> GetMessageTemplateByIdAsync(int messageTemplateId);
    Task<MessageTemplate?> GetMessageTemplateByNameAsync(string messageTemplateName, int storeId);
    Task<IList<MessageTemplate>> GetAllMessageTemplatesAsync(int storeId);
    Task InsertMessageTemplateAsync(MessageTemplate messageTemplate);
    Task UpdateMessageTemplateAsync(MessageTemplate messageTemplate);
    Task DeleteMessageTemplateAsync(MessageTemplate messageTemplate);
    Task<MessageTemplate> CopyMessageTemplateAsync(MessageTemplate messageTemplate);
}
