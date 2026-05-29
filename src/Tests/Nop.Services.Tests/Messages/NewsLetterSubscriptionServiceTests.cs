using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Messages;
using NUnit.Framework;
using NSubstitute;

namespace Nop.Services.Tests.Messages 
{
    [TestFixture]
    public class NewsLetterSubscriptionServiceTests : ServiceTest
    {
        private IEventPublisher _eventPublisher;
        private IRepository<NewsLetterSubscription> _newsLetterSubscriptionRepository;
        private IRepository<Customer> _customerRepository;
        private ICustomerService _customerService;
        private IDbContext _dbContext;

        [SetUp]
        public new void SetUp()
        {
            _eventPublisher = Substitute.For<IEventPublisher>();
            _newsLetterSubscriptionRepository = Substitute.For<IRepository<NewsLetterSubscription>>();
            _customerRepository = Substitute.For<IRepository<Customer>>();
            _customerService = Substitute.For<ICustomerService>();
            _dbContext = Substitute.For<IDbContext>();
        }

        /// <summary>
        /// Verifies the active insert triggers subscribe event.
        /// </summary>
        [Test]
        public void VerifyActiveInsertTriggersSubscribeEvent()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };
            service.InsertNewsLetterSubscription(subscription, true);

            _eventPublisher.Received().Publish(new EmailSubscribedEvent(subscription));
        }

        /// <summary>
        /// Verifies the delete triggers unsubscribe event.
        /// </summary>
        [Test]
        public void VerifyDeleteTriggersUnsubscribeEvent()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };
            service.DeleteNewsLetterSubscription(subscription, true);

            _eventPublisher.Received().Publish(new EmailUnsubscribedEvent(subscription));
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
            _newsLetterSubscriptionRepository.GetById(Arg.Any<object>()).Returns(originalSubscription);

            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@somenewdomain.com" };
            service.UpdateNewsLetterSubscription(subscription, true);

            _eventPublisher.Received().Publish(new EmailUnsubscribedEvent(originalSubscription));
            _eventPublisher.Received().Publish(new EmailSubscribedEvent(subscription));
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
            _newsLetterSubscriptionRepository.GetById(Arg.Any<object>()).Returns(originalSubscription);

            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            var subscription = new NewsLetterSubscription { Active = true, Email = "test@test.com" };

            service.UpdateNewsLetterSubscription(subscription, true);

            _eventPublisher.Received().Publish(new EmailSubscribedEvent(subscription));
        }

        /// <summary>
        /// Verifies the insert event is fired.
        /// </summary>
        [Test]
        public void VerifyInsertEventIsFired()
        {
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            service.InsertNewsLetterSubscription(new NewsLetterSubscription { Email = "test@test.com" });

            _eventPublisher.Received().EntityInserted(Arg.Any<NewsLetterSubscription>());
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

            _newsLetterSubscriptionRepository.GetById(Arg.Any<object>()).Returns(originalSubscription);
            var service = new NewsLetterSubscriptionService(_dbContext, _newsLetterSubscriptionRepository,
                _customerRepository, _eventPublisher, _customerService);

            service.UpdateNewsLetterSubscription(new NewsLetterSubscription { Email = "test@test.com" });

            _eventPublisher.Received().EntityUpdated(Arg.Any<NewsLetterSubscription>());
        }
    }
}