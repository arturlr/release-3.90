using System;
using System.Linq;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Events
{
    [TestFixture]
    public class EventsTests
    {
        private NopEngine _engine;
        private IEventPublisher _eventPublisher;
        private IEngine _previousEngine;

        [OneTimeSetUp]
        public void SetUp()
        {
            _engine = new NopEngine();
            _engine.Initialize(new NopConfig());
            _eventPublisher = _engine.Resolve<IEventPublisher>();

            //Task 17.1: publishing resolves consumers through the STATIC EngineContext.Current
            //(SubscriptionService.GetSubscriptions<T>() -> EngineContext.Current.ResolveAll<IConsumer<T>>()),
            //NOT through _engine directly. Under MVC 5 / 3.90 EngineContext.Current lazily became a
            //real assembly-scanning NopEngine, so DateTimeConsumer was discovered. On net10.0 the
            //assembly-level TestEngineSetup installs a lightweight fake engine (for the validators'
            //display-name resolution) whose ResolveAll returns nothing, which would starve the
            //publisher of consumers. So this fixture makes EngineContext.Current ITS OWN real engine
            //for the duration of the fixture — which is also the honest arrangement: the engine that
            //publishes is the current engine — and restores the previous one in teardown so later
            //fixtures see the harness engine again. (NopEngine.Initialize does not touch
            //EngineContext, so this must be done explicitly.)
            _previousEngine = EngineContext.Current;
            EngineContext.Replace(_engine);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            EngineContext.Replace(_previousEngine);
        }

        [Test]
        public void Can_find_consumers()
        {
            var types = _engine.ResolveAll<IConsumer<DateTime>>().ToList();
            Assert.AreEqual(1, types.Count);
            Assert.IsInstanceOf<DateTimeConsumer>(types[0]);
        }

        [Test]
        public void Can_publish_event()
        {
            var oldDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(7));
            DateTimeConsumer.DateTime = oldDateTime;

            var newDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(5));
            _eventPublisher.Publish(newDateTime);

            Assert.AreEqual(DateTimeConsumer.DateTime, newDateTime);
        }
    }
}
