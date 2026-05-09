using Moq;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Messages;
using NUnit.Framework;

namespace Nop.Services.Tests.Messages 
{
    [TestFixture]
    public class NewsLetterSubscriptionServiceTests : ServiceTest
    {
        private Mock<IEventPublisher> _eventPublisherMock;
        private IRepository<NewsLetterSubscription> _newsLetterSubscriptionRepository;
        private IRepository<Customer> _customerRepository;
        private ICustomerService _customerService;
        private IDbContext _dbContext;

        [SetUp]
        public new void SetUp()
        {
            _eventPublisherMock = new Mock<IEventPublisher>();
            _newsLetterSubscriptionRepository = new Mock<IRepository<NewsLetterSubscription>>().Object;
            _customerRepository = new Mock<IRepository<Customer>>().Object;
            _customerService = new Mock<ICustomerService>().Object;
            _dbContext = new Mock<IDbContext>().Object;
        }

        /// <summary>
        /// Verifies the active insert triggers subscribe event.
        /// </summary>
        [Test]
        public void VerifyActiveInsertTriggersSubscribeEvent()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };
            service.InsertNewsLetterSubscription(subscription, true);

            _eventPublisherMock.Verify(x => x.Publish(It.Is<EmailSubscribedEvent>(e => e != null)));
        }

        /// <summary>
        /// Verifies the delete triggers unsubscribe event.
        /// </summary>
        [Test]
        public void VerifyDeleteTriggersUnsubscribeEvent()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };
            service.DeleteNewsLetterSubscription(subscription, true);

            _eventPublisherMock.Verify(x => x.Publish(It.Is<EmailUnsubscribedEvent>(e => e != null)));
        }

        /// <summary>
        /// Verifies the email update triggers unsubscribe and subscribe event.
        /// </summary>
        [Test]
        [Ignore("Ignoring until a solution to the IDbContext methods are found. -SRS")]
        public void VerifyEmailUpdateTriggersUnsubscribeAndSubscribeEvent()
        {
            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };
            var newsLetterSubscriptionRepoMock = new Mock<IRepository<NewsLetterSubscription>>();
            newsLetterSubscriptionRepoMock.Setup(m => m.GetById(It.IsAny<object>())).Returns(originalSubscription);

            var service = new NewsLetterSubscriptionService(_dbContext, newsLetterSubscriptionRepoMock.Object,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@somenewdomain.com" };
            service.UpdateNewsLetterSubscription(subscription, true);

            _eventPublisherMock.Verify(x => x.Publish(It.Is<EmailUnsubscribedEvent>(e => e != null)));
            _eventPublisherMock.Verify(x => x.Publish(It.Is<EmailSubscribedEvent>(e => e != null)));
        }

        /// <summary>
        /// Verifies the inactive to active update triggers subscribe event.
        /// </summary>
        [Test]
        [Ignore("Ignoring until a solution to the IDbContext methods are found. -SRS")]
        public void VerifyInactiveToActiveUpdateTriggersSubscribeEvent()
        {
            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription { Active = false, Email = "test@test.com" };
            var newsLetterSubscriptionRepoMock = new Mock<IRepository<NewsLetterSubscription>>();
            newsLetterSubscriptionRepoMock.Setup(m => m.GetById(It.IsAny<object>())).Returns(originalSubscription);

            var service = new NewsLetterSubscriptionService(_dbContext, newsLetterSubscriptionRepoMock.Object,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };

            service.UpdateNewsLetterSubscription(subscription, true);

            _eventPublisherMock.Verify(x => x.Publish(It.Is<EmailSubscribedEvent>(e => e != null)));
        }

        /// <summary>
        /// Verifies the insert event is fired.
        /// </summary>
        [Test]
        public void VerifyInsertEventIsFired()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            service.InsertNewsLetterSubscription(new NewsLetterSubscription { Email = "test@test.com" });

            _eventPublisherMock.Verify(x => x.EntityInserted(It.IsAny<NewsLetterSubscription>()));
        }

        /// <summary>
        /// Verifies the update event is fired.
        /// </summary>
        [Test]
        [Ignore("Ignoring until a solution to the IDbContext methods are found. -SRS")]
        public void VerifyUpdateEventIsFired()
        {
            //Prepare the original result
            var originalSubscription = new NewsLetterSubscription { Active = false, Email = "test@test.com" };

            var newsLetterSubscriptionRepoMock = new Mock<IRepository<NewsLetterSubscription>>();
            newsLetterSubscriptionRepoMock.Setup(m => m.GetById(It.IsAny<object>())).Returns(originalSubscription);
            var service = new NewsLetterSubscriptionService(_dbContext, newsLetterSubscriptionRepoMock.Object,
                _customerRepository, _eventPublisherMock.Object, _customerService);

            service.UpdateNewsLetterSubscription(new NewsLetterSubscription { Email = "test@test.com" });

            _eventPublisherMock.Verify(x => x.EntityUpdated(It.IsAny<NewsLetterSubscription>()));
        }
    }
}
