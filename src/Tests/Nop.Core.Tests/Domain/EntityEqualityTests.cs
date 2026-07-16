using Nop.Core.Domain.Catalog;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Nop.Core.Tests.Domain
{
    [TestFixture]
    public class EntityEqualityTests
    {
        [Test]
        public void Two_transient_entities_should_not_be_equal() {
            
            var p1 = new Product();
            var p2 = new Product();

            ClassicAssert.AreNotEqual(p1, p2, "Different transient entities should not be equal");
        }

        [Test]
        public void Two_references_to_same_transient_entity_should_be_equal() {
            
            var p1 = new Product();
            var p2 = p1;

            ClassicAssert.AreEqual(p1, p2, "Two references to the same transient entity should be equal");
        }

        [Test]
        public void Two_references_with_the_same_id_should_be_equal() {
            
            int id = 10;
            var p1 = new Product { Id = id };
            var p2 = new Product { Id = id };

            ClassicAssert.AreEqual(p1, p2, "Entities with the same id should be equal");
        }

        [Test]
        public void Entities_with_different_id_should_not_be_equal() {
            
            var p1 = new Product { Id = 2 };
            var p2 = new Product { Id = 5 };

            ClassicAssert.AreNotEqual(p1, p2, "Entities with different ids should not be equal");
        }

        [Test]
        public void Entity_should_not_equal_transient_entity() {
            
            var p1 = new Product { Id = 1 };
            var p2 = new Product();

            ClassicAssert.AreNotEqual(p1, p2, "Entity and transient entity should not be equal");
        }

        [Test]
        public void Entities_with_same_id_but_different_type_should_not_be_equal() {
            int id = 10;
            var p1 = new Product { Id = id };

            var c1 = new Category { Id = id };

            ClassicAssert.AreNotEqual(p1, c1, "Entities of different types should not be equal, even if they have the same id");
        }

        [Test]
        public void Equality_works_using_operators() {

            var p1 = new Product { Id = 1 };
            var p2 = new Product { Id = 1 };

            ClassicAssert.IsTrue(p1 == p2);

            var p3 = new Product();

            ClassicAssert.IsTrue(p1 != p3);
        }
    }

}
