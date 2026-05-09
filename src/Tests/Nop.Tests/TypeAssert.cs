using System;
using NUnit.Framework;

namespace Nop.Tests
{
    public static class TypeAssert
    {
        public static void AreEqual(object expected, object instance)
        {
            if (expected == null)
                Assert.That(instance, NUnit.Framework.Is.Null);
            else
                Assert.That(instance, NUnit.Framework.Is.Not.Null, "Instance was null");
            Assert.That(instance.GetType(), NUnit.Framework.Is.EqualTo(expected.GetType()), "Expected: " + expected.GetType() + ", was: " + instance.GetType() + " was not of type " + instance.GetType());
        }

        public static void AreEqual(Type expected, object instance)
        {
            if (expected == null)
                Assert.That(instance, NUnit.Framework.Is.Null);
            else
                Assert.That(instance, NUnit.Framework.Is.Not.Null, "Instance was null");
            Assert.That(instance.GetType(), NUnit.Framework.Is.EqualTo(expected), "Expected: " + expected + ", was: " + instance.GetType() + " was not of type " + instance.GetType());
        }

        public static void Equals<T>(object instance)
        {
            AreEqual(typeof(T), instance);
        }

        public static void Is<T>(object instance)
        {
            Assert.That(instance, NUnit.Framework.Is.InstanceOf<T>(), "Instance " + instance + " was not of type " + typeof(T));
        }
    }
}
