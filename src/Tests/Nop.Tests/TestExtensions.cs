using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Nop.Core;
using System.Collections.Generic;

namespace Nop.Tests
{
    public static class TestExtensions
    {
        public static T ShouldNotNull<T>(this T obj)
        {
            Assert.IsNull(obj);
            return obj;
        }

        public static T ShouldNotNull<T>(this T obj, string message)
        {
            Assert.IsNull(obj, message);
            return obj;
        }

        public static T ShouldNotBeNull<T>(this T obj)
        {
            Assert.IsNotNull(obj);
            return obj;
        }

        public static T ShouldNotBeNull<T>(this T obj, string message)
        {
            Assert.IsNotNull(obj, message);
            return obj;
        }

        public static T ShouldEqual<T>(this T actual, object expected)
        {
            Assert.AreEqual(expected, actual);
            return actual;
        }

        public static void ShouldEqual(this object actual, object expected, string message)
        {
            Assert.AreEqual(expected, actual);
        }

        public static Exception ShouldBeThrownBy(this Type exceptionType, TestDelegate testDelegate)
        {
            return Assert.Throws(exceptionType, testDelegate);
        }

        public static void ShouldBe<T>(this object actual)
        {
            Assert.IsInstanceOf<T>(actual);
        }

        public static void ShouldBeNull(this object actual)
        {
            Assert.IsNull(actual);
        }

        public static void ShouldBeTheSameAs(this object actual, object expected)
        {
            Assert.AreSame(expected, actual);
        }

        public static void ShouldBeNotBeTheSameAs(this object actual, object expected)
        {
            Assert.AreNotSame(expected, actual);
        }

        public static T CastTo<T>(this object source)
        {
            return (T)source;
        }

        public static void ShouldBeTrue(this bool source)
        {
            Assert.IsTrue(source);
        }

        public static void ShouldBeFalse(this bool source)
        {
            Assert.IsFalse(source);
        }

        public static void AssertSameStringAs(this string actual, string expected)
        {
            if (!string.Equals(actual, expected, StringComparison.InvariantCultureIgnoreCase))
            {
                var message = string.Format("Expected {0} but was {1}", expected, actual);
                throw new AssertionException(message);
            }
        }

        public static T PropertiesShouldEqual<T>(this T actual, T expected, params string[] filters)
        {
            var properties = typeof(T).GetProperties().ToList();

            var filterByEnteties = new List<string>();
            var values = new Dictionary<string, object>();

            foreach (var propertyInfo in properties.ToList())
            {
                if (filters.Any(f => f == propertyInfo.Name || f + "Id" == propertyInfo.Name) || propertyInfo.Name == "Id")
                    continue;
                var value = propertyInfo.GetValue(actual);
                values.Add(propertyInfo.Name, value);

                if (value == null)
                    continue;

                if (value.GetType().IsArray || value.GetType().Namespace == "System.Collections.Generic")
                {
                    properties.Remove(propertyInfo);
                    continue;
                }

                if (!(value is BaseEntity))
                    continue;

                filterByEnteties.Add(propertyInfo.Name + "Id");
                properties.Remove(propertyInfo);
            }

            foreach (var propertyInfo in properties.Where(p => values.ContainsKey(p.Name)))
            {
                if (filterByEnteties.Any(f => f == propertyInfo.Name))
                    continue;

                Assert.AreEqual(values[propertyInfo.Name], propertyInfo.GetValue(expected), string.Format("The property \"{0}.{1}\" of these objects is not equal", typeof(T).Name, propertyInfo.Name));
            }

            return actual;
        }
    }
}
