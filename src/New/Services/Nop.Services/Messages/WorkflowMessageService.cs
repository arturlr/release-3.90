using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Stores;

namespace Nop.Services.Messages;

public class WorkflowMessageService : IWorkflowMessageService
{
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly IQueuedEmailService _queuedEmailService;
    private readonly ILanguageService _languageService;
    private readonly ITokenizer _tokenizer;
    private readonly IEmailAccountService _emailAccountService;
    private readonly IMessageTokenProvider _messageTokenProvider;
    private readonly IStoreContext _storeContext;
    private readonly IStoreService _storeService;
    private readonly EmailAccountSettings _emailAccountSettings;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRepository<Customer> _customerRepository;

    public WorkflowMessageService(
        IMessageTemplateService messageTemplateService,
        IQueuedEmailService queuedEmailService,
        ILanguageService languageService,
        ITokenizer tokenizer,
        IEmailAccountService emailAccountService,
        IMessageTokenProvider messageTokenProvider,
        IStoreContext storeContext,
        IStoreService storeService,
        EmailAccountSettings emailAccountSettings,
        IEventPublisher eventPublisher,
        IRepository<Customer> customerRepository)
    {
        _messageTemplateService = messageTemplateService;
        _queuedEmailService = queuedEmailService;
        _languageService = languageService;
        _tokenizer = tokenizer;
        _emailAccountService = emailAccountService;
        _messageTokenProvider = messageTokenProvider;
        _storeContext = storeContext;
        _storeService = storeService;
        _emailAccountSettings = emailAccountSettings;
        _eventPublisher = eventPublisher;
        _customerRepository = customerRepository;
    }

    #region Utilities

    private async Task<MessageTemplate?> GetActiveMessageTemplateAsync(string name, int storeId)
    {
        var mt = await _messageTemplateService.GetMessageTemplateByNameAsync(name, storeId);
        return mt is { IsActive: true } ? mt : null;
    }

    private async Task<EmailAccount> GetEmailAccountAsync(MessageTemplate mt, int languageId)
    {
        var ea = await _emailAccountService.GetEmailAccountByIdAsync(mt.EmailAccountId);
        ea ??= await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
        ea ??= (await _emailAccountService.GetAllEmailAccountsAsync()).FirstOrDefault();
        return ea!;
    }

    private async Task<int> EnsureLanguageIsActiveAsync(int languageId, int storeId)
    {
        var lang = await _languageService.GetLanguageByIdAsync(languageId);
        if (lang == null || !lang.Published)
            lang = (await _languageService.GetAllLanguagesAsync(storeId: storeId)).FirstOrDefault();
        if (lang == null || !lang.Published)
            lang = (await _languageService.GetAllLanguagesAsync()).FirstOrDefault();
        if (lang == null)
            throw new NopException("No active language could be loaded");
        return lang.Id;
    }

    #endregion

    #region Customer workflow

    public async Task<int> SendCustomerRegisteredNotificationMessageAsync(Customer customer, int languageId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.CustomerRegisteredNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendCustomerWelcomeMessageAsync(Customer customer, int languageId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.CustomerWelcomeMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    public async Task<int> SendCustomerEmailValidationMessageAsync(Customer customer, int languageId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.CustomerEmailValidationMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    public async Task<int> SendCustomerEmailRevalidationMessageAsync(Customer customer, int languageId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.CustomerEmailRevalidationMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    public async Task<int> SendCustomerPasswordRecoveryMessageAsync(Customer customer, int languageId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.CustomerPasswordRecoveryMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    #endregion

    #region Newsletter workflow

    public async Task<int> SendNewsLetterSubscriptionActivationMessageAsync(NewsLetterSubscription subscription, int languageId)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var store = (await _storeService.GetStoreByIdAsync(subscription.StoreId)) ?? _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewsletterSubscriptionActivationMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddNewsLetterSubscriptionTokensAsync(tokens, subscription);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, subscription.Email!, string.Empty);
    }

    public async Task<int> SendNewsLetterSubscriptionDeactivationMessageAsync(NewsLetterSubscription subscription, int languageId)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var store = (await _storeService.GetStoreByIdAsync(subscription.StoreId)) ?? _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewsletterSubscriptionDeactivationMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddNewsLetterSubscriptionTokensAsync(tokens, subscription);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, subscription.Email!, string.Empty);
    }

    #endregion

    #region Misc notifications

    public async Task<int> SendContactUsMessageAsync(int languageId, string senderEmail, string senderName, string? subject, string body)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ContactUsMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        tokens.Add(new Token("ContactUs.SenderEmail", senderEmail));
        tokens.Add(new Token("ContactUs.SenderName", senderName));
        tokens.Add(new Token("ContactUs.Body", body, neverHtmlEncoded: true));
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!,
            replyToEmailAddress: senderEmail, replyToName: senderName, subject: subject);
    }

    public async Task<int> SendContactVendorMessageAsync(Vendor vendor, int languageId, string senderEmail, string senderName, string? subject, string body)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ContactVendorMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        tokens.Add(new Token("ContactUs.SenderEmail", senderEmail));
        tokens.Add(new Token("ContactUs.SenderName", senderName));
        tokens.Add(new Token("ContactUs.Body", body, neverHtmlEncoded: true));
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, vendor.Email!, vendor.Name!,
            replyToEmailAddress: senderEmail, replyToName: senderName, subject: subject);
    }

    public async Task<int> SendTestEmailAsync(int messageTemplateId, string sendToEmail, List<Token> tokens, int languageId)
    {
        var mt = await _messageTemplateService.GetMessageTemplateByIdAsync(messageTemplateId)
            ?? throw new NopException("Message template not found");
        var ea = await GetEmailAccountAsync(mt, languageId);
        return await SendNotificationAsync(mt, ea, languageId, tokens, sendToEmail, string.Empty);
    }

    #endregion

    #region SendNotification (core)

    public async Task<int> SendNotificationAsync(MessageTemplate messageTemplate, EmailAccount emailAccount,
        int languageId, IEnumerable<Token> tokens, string toEmailAddress, string toName,
        string? attachmentFilePath = null, string? attachmentFileName = null,
        string? replyToEmailAddress = null, string? replyToName = null,
        string? fromEmail = null, string? fromName = null, string? subject = null)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);
        ArgumentNullException.ThrowIfNull(emailAccount);

        var tokenList = tokens.ToList();

        var subjectTemplate = subject ?? messageTemplate.Subject ?? string.Empty;
        var bodyTemplate = messageTemplate.Body ?? string.Empty;

        var resolvedSubject = _tokenizer.Replace(subjectTemplate, tokenList, false);
        var resolvedBody = _tokenizer.Replace(bodyTemplate, tokenList, true);

        var bcc = messageTemplate.BccEmailAddresses?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var from = fromEmail ?? emailAccount.Email ?? string.Empty;
        var fromDisplayName = fromName ?? emailAccount.DisplayName ?? string.Empty;

        // calculate delay
        DateTime? dontSendBefore = null;
        if (messageTemplate.DelayBeforeSend.HasValue && messageTemplate.DelayBeforeSend.Value > 0)
        {
            var delayHours = messageTemplate.DelayPeriod.ToHours(messageTemplate.DelayBeforeSend.Value);
            dontSendBefore = DateTime.UtcNow.AddHours(delayHours);
        }

        var queuedEmail = new QueuedEmail
        {
            PriorityId = (int)QueuedEmailPriority.High,
            From = from,
            FromName = fromDisplayName,
            To = toEmailAddress,
            ToName = toName,
            ReplyTo = replyToEmailAddress,
            ReplyToName = replyToName,
            CC = null,
            Bcc = bcc != null ? string.Join(';', bcc) : null,
            Subject = resolvedSubject,
            Body = resolvedBody,
            AttachmentFilePath = attachmentFilePath,
            AttachmentFileName = attachmentFileName,
            AttachedDownloadId = messageTemplate.AttachedDownloadId,
            CreatedOnUtc = DateTime.UtcNow,
            EmailAccountId = emailAccount.Id,
            DontSendBeforeDateUtc = dontSendBefore
        };

        await _queuedEmailService.InsertQueuedEmailAsync(queuedEmail);
        return queuedEmail.Id;
    }

    #endregion

    #region Order workflow (minimal — enriched when IOrderService [4.9] is built)

    public async Task<int> SendOrderPlacedStoreOwnerNotificationAsync(Order order, int languageId)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.OrderPlacedStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendOrderPlacedCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.OrderPlacedCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty, attachmentFilePath, attachmentFileName);
    }

    public async Task<int> SendOrderPlacedVendorNotificationAsync(Order order, Vendor vendor, int languageId)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.OrderPlacedVendorNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId, vendor.Id);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, vendor.Email!, vendor.Name!);
    }

    public Task<int> SendOrderPaidStoreOwnerNotificationAsync(Order order, int languageId) => SendOrderNotificationAsync(order, MessageTemplateSystemNames.OrderPaidStoreOwnerNotification, languageId, toOwner: true);
    public Task<int> SendOrderPaidCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null) => SendOrderNotificationAsync(order, MessageTemplateSystemNames.OrderPaidCustomerNotification, languageId, attachmentFilePath: attachmentFilePath, attachmentFileName: attachmentFileName);
    public Task<int> SendOrderPaidVendorNotificationAsync(Order order, Vendor vendor, int languageId) => SendOrderNotificationAsync(order, MessageTemplateSystemNames.OrderPaidVendorNotification, languageId, vendor: vendor);
    public Task<int> SendOrderCompletedCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null) => SendOrderNotificationAsync(order, MessageTemplateSystemNames.OrderCompletedCustomerNotification, languageId, attachmentFilePath: attachmentFilePath, attachmentFileName: attachmentFileName);
    public Task<int> SendOrderCancelledCustomerNotificationAsync(Order order, int languageId) => SendOrderNotificationAsync(order, MessageTemplateSystemNames.OrderCancelledCustomerNotification, languageId);

    public async Task<int> SendOrderRefundedStoreOwnerNotificationAsync(Order order, decimal refundedAmount, int languageId)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.OrderRefundedStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId);
        await _messageTokenProvider.AddOrderRefundedTokensAsync(tokens, order, refundedAmount);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendOrderRefundedCustomerNotificationAsync(Order order, decimal refundedAmount, int languageId)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.OrderRefundedCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId);
        await _messageTokenProvider.AddOrderRefundedTokensAsync(tokens, order, refundedAmount);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty);
    }

    public async Task<int> SendNewOrderNoteAddedCustomerNotificationAsync(OrderNote orderNote, int languageId)
    {
        ArgumentNullException.ThrowIfNull(orderNote);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewOrderNoteAddedCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderNoteTokensAsync(tokens, orderNote);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public Task<int> SendRecurringPaymentCancelledStoreOwnerNotificationAsync(RecurringPayment rp, int languageId) => SendRecurringPaymentNotificationAsync(rp, MessageTemplateSystemNames.RecurringPaymentCancelledStoreOwnerNotification, languageId, toOwner: true);
    public Task<int> SendRecurringPaymentCancelledCustomerNotificationAsync(RecurringPayment rp, int languageId) => SendRecurringPaymentNotificationAsync(rp, MessageTemplateSystemNames.RecurringPaymentCancelledCustomerNotification, languageId);
    public Task<int> SendRecurringPaymentFailedCustomerNotificationAsync(RecurringPayment rp, int languageId) => SendRecurringPaymentNotificationAsync(rp, MessageTemplateSystemNames.RecurringPaymentFailedCustomerNotification, languageId);

    #endregion

    #region Shipment, Return Request, Forum, Send-to-friend, Misc

    public async Task<int> SendShipmentSentCustomerNotificationAsync(Shipment shipment, int languageId)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ShipmentSentCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddShipmentTokensAsync(tokens, shipment, languageId);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendShipmentDeliveredCustomerNotificationAsync(Shipment shipment, int languageId)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ShipmentDeliveredCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddShipmentTokensAsync(tokens, shipment, languageId);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendNewReturnRequestStoreOwnerNotificationAsync(ReturnRequest rr, OrderItem oi, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewReturnRequestStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddReturnRequestTokensAsync(tokens, rr, oi);
        var customer = _customerRepository.GetById(rr.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendNewReturnRequestCustomerNotificationAsync(ReturnRequest rr, OrderItem oi, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewReturnRequestCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddReturnRequestTokensAsync(tokens, rr, oi);
        var customer = _customerRepository.GetById(rr.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty);
    }

    public async Task<int> SendReturnRequestStatusChangedCustomerNotificationAsync(ReturnRequest rr, OrderItem oi, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ReturnRequestStatusChangedCustomerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddReturnRequestTokensAsync(tokens, rr, oi);
        var customer = _customerRepository.GetById(rr.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty);
    }

    public async Task<int> SendNewForumTopicMessageAsync(Customer customer, ForumTopic forumTopic, Forum forum, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewForumTopicMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddForumTopicTokensAsync(tokens, forumTopic);
        await _messageTokenProvider.AddForumTokensAsync(tokens, forum);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    public async Task<int> SendNewForumPostMessageAsync(Customer customer, ForumPost forumPost, ForumTopic forumTopic, Forum forum, int friendlyForumTopicPageIndex, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewForumPostMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddForumPostTokensAsync(tokens, forumPost);
        await _messageTokenProvider.AddForumTopicTokensAsync(tokens, forumTopic, friendlyForumTopicPageIndex);
        await _messageTokenProvider.AddForumTokensAsync(tokens, forum);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer.Email!, string.Empty);
    }

    public async Task<int> SendPrivateMessageNotificationAsync(PrivateMessage pm, int languageId)
    {
        var store = (await _storeService.GetStoreByIdAsync(pm.StoreId)) ?? _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.PrivateMessageNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddPrivateMessageTokensAsync(tokens, pm);
        var customer = _customerRepository.GetById(pm.ToCustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty);
    }

    public async Task<int> SendProductEmailAFriendMessageAsync(Customer customer, int languageId, Product product, string customerEmail, string friendsEmail, string personalMessage)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.EmailAFriendMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _messageTokenProvider.AddProductTokensAsync(tokens, product, languageId);
        tokens.Add(new Token("EmailAFriend.PersonalMessage", personalMessage, neverHtmlEncoded: true));
        tokens.Add(new Token("EmailAFriend.Email", customerEmail));
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, friendsEmail, string.Empty);
    }

    public async Task<int> SendWishlistEmailAFriendMessageAsync(Customer customer, int languageId, string customerEmail, string friendsEmail, string personalMessage)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.WishlistToFriendMessage, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        tokens.Add(new Token("Wishlist.PersonalMessage", personalMessage, neverHtmlEncoded: true));
        tokens.Add(new Token("Wishlist.Email", customerEmail));
        tokens.Add(new Token("Wishlist.URLForCustomer", $"{store.Url}wishlist/{customer.CustomerGuid}", neverHtmlEncoded: true));
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, friendsEmail, string.Empty);
    }

    public async Task<int> SendNewVendorAccountApplyStoreOwnerNotificationAsync(Customer customer, Vendor vendor, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewVendorAccountApplyStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _messageTokenProvider.AddVendorTokensAsync(tokens, vendor);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendVendorInformationChangeNotificationAsync(Vendor vendor, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.VendorInformationChangeNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddVendorTokensAsync(tokens, vendor);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendProductReviewNotificationMessageAsync(ProductReview productReview, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.ProductReviewNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddProductReviewTokensAsync(tokens, productReview);
        var customer = _customerRepository.GetById(productReview.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendGiftCardNotificationAsync(GiftCard giftCard, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.GiftCardNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddGiftCardTokensAsync(tokens, giftCard);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, giftCard.RecipientEmail!, giftCard.RecipientName!);
    }

    public async Task<int> SendQuantityBelowStoreOwnerNotificationAsync(Product product, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.QuantityBelowStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddProductTokensAsync(tokens, product, languageId);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendQuantityBelowStoreOwnerNotificationAsync(ProductAttributeCombination combination, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.QuantityBelowAttributeCombinationStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddAttributeCombinationTokensAsync(tokens, combination, languageId);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendNewVatSubmittedStoreOwnerNotificationAsync(Customer customer, string vatName, string vatAddress, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewVatSubmittedStoreOwnerNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        tokens.Add(new Token("VatValidationResult.Name", vatName));
        tokens.Add(new Token("VatValidationResult.Address", vatAddress));
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendBlogCommentNotificationMessageAsync(BlogComment blogComment, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.BlogCommentNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddBlogCommentTokensAsync(tokens, blogComment);
        var customer = _customerRepository.GetById(blogComment.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendNewsCommentNotificationMessageAsync(NewsComment newsComment, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.NewsCommentNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddNewsCommentTokensAsync(tokens, newsComment);
        var customer = _customerRepository.GetById(newsComment.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, ea.Email!, ea.DisplayName!);
    }

    public async Task<int> SendBackInStockNotificationAsync(BackInStockSubscription subscription, int languageId)
    {
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(MessageTemplateSystemNames.BackInStockNotification, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddBackInStockTokensAsync(tokens, subscription);
        var customer = _customerRepository.GetById(subscription.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens, customer?.Email ?? string.Empty, string.Empty);
    }

    #endregion

    #region Private helpers

    private async Task<int> SendOrderNotificationAsync(Order order, string templateName, int languageId,
        bool toOwner = false, Vendor? vendor = null, string? attachmentFilePath = null, string? attachmentFileName = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(templateName, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddOrderTokensAsync(tokens, order, languageId, vendor?.Id ?? 0);
        var customer = _customerRepository.GetById(order.CustomerId);
        if (customer != null) await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));

        string toEmail, toName;
        if (vendor != null) { toEmail = vendor.Email!; toName = vendor.Name!; }
        else if (toOwner) { toEmail = ea.Email!; toName = ea.DisplayName!; }
        else { toEmail = customer?.Email ?? string.Empty; toName = string.Empty; }

        return await SendNotificationAsync(mt, ea, languageId, tokens, toEmail, toName, attachmentFilePath, attachmentFileName);
    }

    private async Task<int> SendRecurringPaymentNotificationAsync(RecurringPayment rp, string templateName, int languageId, bool toOwner = false)
    {
        ArgumentNullException.ThrowIfNull(rp);
        var store = _storeContext.CurrentStore;
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);
        var mt = await GetActiveMessageTemplateAsync(templateName, store.Id);
        if (mt == null) return 0;
        var ea = await GetEmailAccountAsync(mt, languageId);
        var tokens = new List<Token>();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, ea);
        await _messageTokenProvider.AddRecurringPaymentTokensAsync(tokens, rp);
        await _eventPublisher.PublishAsync(new MessageTokensAddedEvent<Token>(mt, tokens));
        return await SendNotificationAsync(mt, ea, languageId, tokens,
            toOwner ? ea.Email! : ea.Email!, toOwner ? ea.DisplayName! : string.Empty);
    }

    #endregion
}
