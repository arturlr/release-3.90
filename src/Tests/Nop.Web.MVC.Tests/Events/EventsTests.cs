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

        [OneTimeSetUp]
        public void SetUp()
        {
            _engine = new NopEngine();
            _engine.Initialize(new NopConfig());
            _eventPublisher = _engine.Resolve<IEventPublisher>();
        }

        [Test]
        public void Can_find_consumers()
        {
            var types = _engine.ResolveAll<IConsumer<DateTime>>().ToList();
            Assert.That(types.Count, Is.EqualTo(1));
            Assert.That(types[0], Is.InstanceOf<DateTimeConsumer>());
        }

        [Test]
        public void Can_publish_event()
        {
            var oldDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(7));
            DateTimeConsumer.DateTime = oldDateTime;

            var newDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(5));
            _eventPublisher.Publish(newDateTime);

            Assert.That(DateTimeConsumer.DateTime, Is.EqualTo(newDateTime));
        }
    }
}
