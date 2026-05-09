using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// EF Core compatible base class for entity type configurations.
    /// Provides EF6-style fluent API methods that delegate to EF Core's EntityTypeBuilder.
    /// Subclass constructors call fluent API methods which are stored as deferred actions
    /// and replayed when EF Core calls Configure().
    /// </summary>
    public abstract class NopEntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : class
    {
        private readonly List<Action<EntityTypeBuilder<T>>> _configurations = new List<Action<EntityTypeBuilder<T>>>();

        protected NopEntityTypeConfiguration()
        {
        }

        /// <summary>
        /// IEntityTypeConfiguration implementation - called by EF Core
        /// </summary>
        public void Configure(EntityTypeBuilder<T> builder)
        {
            foreach (var config in _configurations)
            {
                config(builder);
            }
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code
        /// </summary>
        protected virtual void PostInitialize()
        {
        }

        internal List<Action<EntityTypeBuilder<T>>> Configurations => _configurations;

        #region EF6-compatible fluent API methods

        protected void ToTable(string tableName)
        {
            _configurations.Add(b => b.ToTable(tableName));
        }

        protected void HasKey<TKey>(Expression<Func<T, TKey>> keyExpression)
        {
            // Convert Expression<Func<T, TKey>> to Expression<Func<T, object>> for EF Core
            var converted = Expression.Lambda<Func<T, object>>(
                Expression.Convert(keyExpression.Body, typeof(object)),
                keyExpression.Parameters);
            _configurations.Add(b => b.HasKey(converted));
        }

        protected PropertyBuilderAdapter<T, TProperty> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return new PropertyBuilderAdapter<T, TProperty>(propertyExpression, _configurations);
        }

        protected void Ignore<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            // Convert to string property name for Ignore
            var memberName = GetMemberName(propertyExpression);
            if (memberName != null)
            {
                _configurations.Add(b => b.Ignore(memberName));
            }
        }

        protected RequiredNavigationBuilder<T, TRelated> HasRequired<TRelated>(Expression<Func<T, TRelated>> navigationExpression) where TRelated : class
        {
            return new RequiredNavigationBuilder<T, TRelated>(navigationExpression, _configurations);
        }

        protected OptionalNavigationBuilder<T, TRelated> HasOptional<TRelated>(Expression<Func<T, TRelated>> navigationExpression) where TRelated : class
        {
            // HasOptional without chaining is a no-op in EF Core (optional is the default)
            return new OptionalNavigationBuilder<T, TRelated>(navigationExpression, _configurations);
        }

        protected ManyNavigationBuilder<T, TRelated> HasMany<TRelated>(Expression<Func<T, IEnumerable<TRelated>>> navigationExpression) where TRelated : class
        {
            return new ManyNavigationBuilder<T, TRelated>(navigationExpression, _configurations);
        }

        private static string GetMemberName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;
            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                return unaryMember.Member.Name;
            return null;
        }

        #endregion
    }

    #region Property Builder Adapter

    /// <summary>
    /// Adapter for Property configuration that stores chained calls and applies them when Configure() runs
    /// </summary>
    public class PropertyBuilderAdapter<TEntity, TProperty> where TEntity : class
    {
        private readonly Expression<Func<TEntity, TProperty>> _propertyExpression;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private bool _isRequired;
        private int? _maxLength;
        private bool _isMaxLength;
        private int? _precision;
        private int? _scale;
        private readonly int _configIndex;

        public PropertyBuilderAdapter(Expression<Func<TEntity, TProperty>> propertyExpression, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _propertyExpression = propertyExpression;
            _configurations = configurations;
            _configIndex = configurations.Count;
            // Add a placeholder that will be updated as chain methods are called
            _configurations.Add(BuildAction());
        }

        public PropertyBuilderAdapter<TEntity, TProperty> IsRequired()
        {
            _isRequired = true;
            UpdateConfiguration();
            return this;
        }

        public PropertyBuilderAdapter<TEntity, TProperty> HasMaxLength(int maxLength)
        {
            _maxLength = maxLength;
            UpdateConfiguration();
            return this;
        }

        /// <summary>
        /// EF6 IsMaxLength - marks the property as having maximum length (no length restriction).
        /// In EF Core, this is the default for byte[] columns. We explicitly don't set HasMaxLength.
        /// </summary>
        public PropertyBuilderAdapter<TEntity, TProperty> IsMaxLength()
        {
            _isMaxLength = true;
            UpdateConfiguration();
            return this;
        }

        public PropertyBuilderAdapter<TEntity, TProperty> HasPrecision(int precision, int scale)
        {
            _precision = precision;
            _scale = scale;
            UpdateConfiguration();
            return this;
        }

        private void UpdateConfiguration()
        {
            _configurations[_configIndex] = BuildAction();
        }

        private Action<EntityTypeBuilder<TEntity>> BuildAction()
        {
            var expr = _propertyExpression;
            var isReq = _isRequired;
            var maxLen = _maxLength;
            var isMax = _isMaxLength;
            var prec = _precision;
            var sc = _scale;

            return b =>
            {
                var propBuilder = b.Property(expr);
                if (isReq)
                    propBuilder.IsRequired();
                if (maxLen.HasValue)
                    propBuilder.HasMaxLength(maxLen.Value);
                // IsMaxLength in EF Core is the default (no max length set) - no action needed
                // but we could explicitly set it if needed for documentation purposes
                if (prec.HasValue && sc.HasValue)
                    propBuilder.HasPrecision(prec.Value, sc.Value);
            };
        }
    }

    #endregion

    #region Navigation Builder Adapters

    /// <summary>
    /// Adapter for HasRequired navigation (one-to-many with required principal)
    /// </summary>
    public class RequiredNavigationBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public RequiredNavigationBuilder(Expression<Func<TEntity, TRelated>> navigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _configurations = configurations;
        }

        public RequiredWithManyBuilder<TEntity, TRelated> WithMany(Expression<Func<TRelated, IEnumerable<TEntity>>> collection)
        {
            return new RequiredWithManyBuilder<TEntity, TRelated>(_navigation, collection, _configurations);
        }

        public RequiredWithManyBuilder<TEntity, TRelated> WithMany()
        {
            return new RequiredWithManyBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }
    }

    public class RequiredWithManyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, IEnumerable<TEntity>>> _collection;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public RequiredWithManyBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, IEnumerable<TEntity>>> collection, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _collection = collection;
            _configurations = configurations;
        }

        public RequiredForeignKeyBuilder<TEntity, TRelated> HasForeignKey<TKey>(Expression<Func<TEntity, TKey>> foreignKeyExpression)
        {
            var nav = _navigation;
            var col = _collection;
            // Convert FK expression to use object for EF Core
            var fkConverted = Expression.Lambda<Func<TEntity, object>>(
                Expression.Convert(foreignKeyExpression.Body, typeof(object)),
                foreignKeyExpression.Parameters);

            int idx = _configurations.Count;
            _configurations.Add(b =>
            {
                ReferenceCollectionBuilder<TRelated, TEntity> refBuilder;
                if (col != null)
                    refBuilder = b.HasOne(nav).WithMany(col).IsRequired();
                else
                    refBuilder = b.HasOne(nav).WithMany().IsRequired();
                refBuilder.HasForeignKey(fkConverted);
            });
            return new RequiredForeignKeyBuilder<TEntity, TRelated>(nav, col, fkConverted, _configurations, idx);
        }
    }

    public class RequiredForeignKeyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, IEnumerable<TEntity>>> _collection;
        private readonly Expression<Func<TEntity, object>> _foreignKey;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private readonly int _configIndex;

        public RequiredForeignKeyBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, IEnumerable<TEntity>>> collection, Expression<Func<TEntity, object>> foreignKey, List<Action<EntityTypeBuilder<TEntity>>> configurations, int configIndex)
        {
            _navigation = navigation;
            _collection = collection;
            _foreignKey = foreignKey;
            _configurations = configurations;
            _configIndex = configIndex;
        }

        public RequiredForeignKeyBuilder<TEntity, TRelated> WillCascadeOnDelete(bool cascade)
        {
            var nav = _navigation;
            var col = _collection;
            var fk = _foreignKey;
            _configurations[_configIndex] = b =>
            {
                ReferenceCollectionBuilder<TRelated, TEntity> refBuilder;
                if (col != null)
                    refBuilder = b.HasOne(nav).WithMany(col).IsRequired();
                else
                    refBuilder = b.HasOne(nav).WithMany().IsRequired();
                refBuilder.HasForeignKey(fk);
                refBuilder.OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            };
            return this;
        }
    }

    /// <summary>
    /// Adapter for HasOptional navigation (one-to-many with optional principal, or one-to-one optional)
    /// </summary>
    public class OptionalNavigationBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public OptionalNavigationBuilder(Expression<Func<TEntity, TRelated>> navigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _configurations = configurations;
        }

        public OptionalWithManyBuilder<TEntity, TRelated> WithMany(Expression<Func<TRelated, IEnumerable<TEntity>>> collection)
        {
            return new OptionalWithManyBuilder<TEntity, TRelated>(_navigation, collection, _configurations);
        }

        public OptionalWithManyBuilder<TEntity, TRelated> WithMany()
        {
            return new OptionalWithManyBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }

        /// <summary>
        /// EF6 WithOptionalDependent - configures a one-to-one relationship where the related entity is the dependent.
        /// In EF Core this maps to HasOne().WithOne().
        /// </summary>
        public OptionalDependentBuilder<TEntity, TRelated> WithOptionalDependent(Expression<Func<TRelated, TEntity>> inverseNavigation)
        {
            return new OptionalDependentBuilder<TEntity, TRelated>(_navigation, inverseNavigation, _configurations);
        }

        /// <summary>
        /// EF6 WithOptionalDependent without inverse navigation
        /// </summary>
        public OptionalDependentBuilder<TEntity, TRelated> WithOptionalDependent()
        {
            return new OptionalDependentBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }

        /// <summary>
        /// EF6 WithOptionalPrincipal - configures a one-to-one relationship where the related entity is the principal.
        /// In EF Core this maps to HasOne().WithOne() with FK on current entity.
        /// </summary>
        public OptionalPrincipalBuilder<TEntity, TRelated> WithOptionalPrincipal(Expression<Func<TRelated, TEntity>> inverseNavigation)
        {
            return new OptionalPrincipalBuilder<TEntity, TRelated>(_navigation, inverseNavigation, _configurations);
        }

        /// <summary>
        /// EF6 WithOptionalPrincipal without inverse navigation
        /// </summary>
        public OptionalPrincipalBuilder<TEntity, TRelated> WithOptionalPrincipal()
        {
            return new OptionalPrincipalBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }
    }

    /// <summary>
    /// Builder for optional one-to-one where TRelated is the dependent (has the FK)
    /// EF6: HasOptional(x => x.Related).WithOptionalDependent(y => y.Principal)
    /// EF Core: HasOne(x => x.Related).WithOne(y => y.Principal).HasForeignKey&lt;TRelated&gt;()
    /// </summary>
    public class OptionalDependentBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, TEntity>> _inverseNavigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private readonly int _configIndex;

        public OptionalDependentBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, TEntity>> inverseNavigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _configurations = configurations;
            _configIndex = configurations.Count;

            // Add default configuration (no cascade delete)
            var nav = _navigation;
            var inv = _inverseNavigation;
            _configurations.Add(b =>
            {
                if (inv != null)
                    b.HasOne(nav).WithOne(inv).IsRequired(false);
                else
                    b.HasOne(nav).WithOne().IsRequired(false);
            });
        }

        public OptionalDependentBuilder<TEntity, TRelated> WillCascadeOnDelete(bool cascade)
        {
            var nav = _navigation;
            var inv = _inverseNavigation;
            _configurations[_configIndex] = b =>
            {
                ReferenceReferenceBuilder<TEntity, TRelated> refBuilder;
                if (inv != null)
                    refBuilder = b.HasOne(nav).WithOne(inv).IsRequired(false);
                else
                    refBuilder = b.HasOne(nav).WithOne().IsRequired(false);
                refBuilder.OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            };
            return this;
        }
    }

    /// <summary>
    /// Builder for optional one-to-one where TEntity is the dependent (has the FK)
    /// EF6: HasOptional(x => x.Related).WithOptionalPrincipal(y => y.Dependent)
    /// EF Core: HasOne(x => x.Related).WithOne(y => y.Dependent).HasForeignKey&lt;TEntity&gt;()
    /// </summary>
    public class OptionalPrincipalBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, TEntity>> _inverseNavigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private readonly int _configIndex;

        public OptionalPrincipalBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, TEntity>> inverseNavigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _configurations = configurations;
            _configIndex = configurations.Count;

            var nav = _navigation;
            var inv = _inverseNavigation;
            _configurations.Add(b =>
            {
                if (inv != null)
                    b.HasOne(nav).WithOne(inv).IsRequired(false);
                else
                    b.HasOne(nav).WithOne().IsRequired(false);
            });
        }

        public OptionalPrincipalBuilder<TEntity, TRelated> WillCascadeOnDelete(bool cascade)
        {
            var nav = _navigation;
            var inv = _inverseNavigation;
            _configurations[_configIndex] = b =>
            {
                ReferenceReferenceBuilder<TEntity, TRelated> refBuilder;
                if (inv != null)
                    refBuilder = b.HasOne(nav).WithOne(inv).IsRequired(false);
                else
                    refBuilder = b.HasOne(nav).WithOne().IsRequired(false);
                refBuilder.OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            };
            return this;
        }
    }

    public class OptionalWithManyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, IEnumerable<TEntity>>> _collection;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public OptionalWithManyBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, IEnumerable<TEntity>>> collection, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _collection = collection;
            _configurations = configurations;
        }

        public OptionalForeignKeyBuilder<TEntity, TRelated> HasForeignKey<TKey>(Expression<Func<TEntity, TKey>> foreignKeyExpression)
        {
            var nav = _navigation;
            var col = _collection;
            // Convert FK expression
            var fkConverted = Expression.Lambda<Func<TEntity, object>>(
                Expression.Convert(foreignKeyExpression.Body, typeof(object)),
                foreignKeyExpression.Parameters);

            int idx = _configurations.Count;
            _configurations.Add(b =>
            {
                ReferenceCollectionBuilder<TRelated, TEntity> refBuilder;
                if (col != null)
                    refBuilder = b.HasOne(nav).WithMany(col).IsRequired(false);
                else
                    refBuilder = b.HasOne(nav).WithMany().IsRequired(false);
                refBuilder.HasForeignKey(fkConverted);
            });
            return new OptionalForeignKeyBuilder<TEntity, TRelated>(nav, col, fkConverted, _configurations, idx);
        }
    }

    public class OptionalForeignKeyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, TRelated>> _navigation;
        private readonly Expression<Func<TRelated, IEnumerable<TEntity>>> _collection;
        private readonly Expression<Func<TEntity, object>> _foreignKey;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private readonly int _configIndex;

        public OptionalForeignKeyBuilder(Expression<Func<TEntity, TRelated>> navigation, Expression<Func<TRelated, IEnumerable<TEntity>>> collection, Expression<Func<TEntity, object>> foreignKey, List<Action<EntityTypeBuilder<TEntity>>> configurations, int configIndex)
        {
            _navigation = navigation;
            _collection = collection;
            _foreignKey = foreignKey;
            _configurations = configurations;
            _configIndex = configIndex;
        }

        public OptionalForeignKeyBuilder<TEntity, TRelated> WillCascadeOnDelete(bool cascade)
        {
            var nav = _navigation;
            var col = _collection;
            var fk = _foreignKey;
            _configurations[_configIndex] = b =>
            {
                ReferenceCollectionBuilder<TRelated, TEntity> refBuilder;
                if (col != null)
                    refBuilder = b.HasOne(nav).WithMany(col).IsRequired(false);
                else
                    refBuilder = b.HasOne(nav).WithMany().IsRequired(false);
                refBuilder.HasForeignKey(fk);
                refBuilder.OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            };
            return this;
        }
    }

    /// <summary>
    /// Adapter for HasMany navigation
    /// </summary>
    public class ManyNavigationBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, IEnumerable<TRelated>>> _navigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public ManyNavigationBuilder(Expression<Func<TEntity, IEnumerable<TRelated>>> navigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _configurations = configurations;
        }

        public ManyToManyBuilder<TEntity, TRelated> WithMany(Expression<Func<TRelated, IEnumerable<TEntity>>> inverseNavigation)
        {
            return new ManyToManyBuilder<TEntity, TRelated>(_navigation, inverseNavigation, _configurations);
        }

        public ManyToManyBuilder<TEntity, TRelated> WithMany()
        {
            return new ManyToManyBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }

        public ManyWithRequiredBuilder<TEntity, TRelated> WithRequired(Expression<Func<TRelated, TEntity>> inverseNavigation)
        {
            return new ManyWithRequiredBuilder<TEntity, TRelated>(_navigation, inverseNavigation, _configurations);
        }

        /// <summary>
        /// EF6 WithOptional - configures a one-to-many relationship where the FK is optional (nullable).
        /// In EF Core this maps to HasMany().WithOne() where the FK is nullable by default.
        /// </summary>
        public ManyWithOptionalBuilder<TEntity, TRelated> WithOptional(Expression<Func<TRelated, TEntity>> inverseNavigation)
        {
            return new ManyWithOptionalBuilder<TEntity, TRelated>(_navigation, inverseNavigation, _configurations);
        }

        /// <summary>
        /// EF6 WithOptional without inverse navigation
        /// </summary>
        public ManyWithOptionalBuilder<TEntity, TRelated> WithOptional()
        {
            return new ManyWithOptionalBuilder<TEntity, TRelated>(_navigation, null, _configurations);
        }
    }

    /// <summary>
    /// Builder for HasMany().WithOptional() - one-to-many with optional FK
    /// EF6: HasMany(x => x.Children).WithOptional().HasForeignKey(x => x.ParentId)
    /// EF Core: HasMany(x => x.Children).WithOne().HasForeignKey(x => x.ParentId).IsRequired(false)
    /// </summary>
    public class ManyWithOptionalBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, IEnumerable<TRelated>>> _navigation;
        private readonly Expression<Func<TRelated, TEntity>> _inverseNavigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public ManyWithOptionalBuilder(Expression<Func<TEntity, IEnumerable<TRelated>>> navigation, Expression<Func<TRelated, TEntity>> inverseNavigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _configurations = configurations;
        }

        public ManyWithOptionalForeignKeyBuilder<TEntity, TRelated> HasForeignKey<TKey>(Expression<Func<TRelated, TKey>> foreignKeyExpression)
        {
            var nav = _navigation;
            var inv = _inverseNavigation;
            // Convert FK expression
            var fkConverted = Expression.Lambda<Func<TRelated, object>>(
                Expression.Convert(foreignKeyExpression.Body, typeof(object)),
                foreignKeyExpression.Parameters);

            int idx = _configurations.Count;
            _configurations.Add(b =>
            {
                if (inv != null)
                    b.HasMany(nav).WithOne(inv).HasForeignKey(fkConverted).IsRequired(false);
                else
                    b.HasMany(nav).WithOne().HasForeignKey(fkConverted).IsRequired(false);
            });
            return new ManyWithOptionalForeignKeyBuilder<TEntity, TRelated>(nav, inv, fkConverted, _configurations, idx);
        }
    }

    public class ManyWithOptionalForeignKeyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, IEnumerable<TRelated>>> _navigation;
        private readonly Expression<Func<TRelated, TEntity>> _inverseNavigation;
        private readonly Expression<Func<TRelated, object>> _foreignKey;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;
        private readonly int _configIndex;

        public ManyWithOptionalForeignKeyBuilder(Expression<Func<TEntity, IEnumerable<TRelated>>> navigation, Expression<Func<TRelated, TEntity>> inverseNavigation, Expression<Func<TRelated, object>> foreignKey, List<Action<EntityTypeBuilder<TEntity>>> configurations, int configIndex)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _foreignKey = foreignKey;
            _configurations = configurations;
            _configIndex = configIndex;
        }

        public ManyWithOptionalForeignKeyBuilder<TEntity, TRelated> WillCascadeOnDelete(bool cascade)
        {
            var nav = _navigation;
            var inv = _inverseNavigation;
            var fk = _foreignKey;
            _configurations[_configIndex] = b =>
            {
                if (inv != null)
                    b.HasMany(nav).WithOne(inv).HasForeignKey(fk).IsRequired(false).OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
                else
                    b.HasMany(nav).WithOne().HasForeignKey(fk).IsRequired(false).OnDelete(cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            };
            return this;
        }
    }

    public class ManyToManyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, IEnumerable<TRelated>>> _navigation;
        private readonly Expression<Func<TRelated, IEnumerable<TEntity>>> _inverseNavigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public ManyToManyBuilder(Expression<Func<TEntity, IEnumerable<TRelated>>> navigation, Expression<Func<TRelated, IEnumerable<TEntity>>> inverseNavigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _configurations = configurations;
        }

        public void Map(Action<ManyToManyMapBuilder> configAction)
        {
            var mapBuilder = new ManyToManyMapBuilder();
            configAction(mapBuilder);
            var tableName = mapBuilder.TableName;
            var nav = _navigation;
            var inv = _inverseNavigation;

            _configurations.Add(b =>
            {
                CollectionCollectionBuilder<TRelated, TEntity> manyBuilder;
                if (inv != null)
                    manyBuilder = b.HasMany(nav).WithMany(inv);
                else
                    manyBuilder = b.HasMany(nav).WithMany();

                if (!string.IsNullOrEmpty(tableName))
                {
                    manyBuilder.UsingEntity(tableName);
                }
            });
        }
    }

    public class ManyToManyMapBuilder
    {
        public string TableName { get; private set; }

        public void ToTable(string tableName)
        {
            TableName = tableName;
        }
    }

    public class ManyWithRequiredBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
        private readonly Expression<Func<TEntity, IEnumerable<TRelated>>> _navigation;
        private readonly Expression<Func<TRelated, TEntity>> _inverseNavigation;
        private readonly List<Action<EntityTypeBuilder<TEntity>>> _configurations;

        public ManyWithRequiredBuilder(Expression<Func<TEntity, IEnumerable<TRelated>>> navigation, Expression<Func<TRelated, TEntity>> inverseNavigation, List<Action<EntityTypeBuilder<TEntity>>> configurations)
        {
            _navigation = navigation;
            _inverseNavigation = inverseNavigation;
            _configurations = configurations;
        }

        public ManyWithRequiredForeignKeyBuilder<TEntity, TRelated> HasForeignKey<TKey>(Expression<Func<TRelated, TKey>> foreignKeyExpression)
        {
            var nav = _navigation;
            var inv = _inverseNavigation;
            // Convert FK expression
            var fkConverted = Expression.Lambda<Func<TRelated, object>>(
                Expression.Convert(foreignKeyExpression.Body, typeof(object)),
                foreignKeyExpression.Parameters);

            _configurations.Add(b =>
            {
                b.HasMany(nav).WithOne(inv).HasForeignKey(fkConverted).IsRequired();
            });
            return new ManyWithRequiredForeignKeyBuilder<TEntity, TRelated>();
        }
    }

    public class ManyWithRequiredForeignKeyBuilder<TEntity, TRelated> where TEntity : class where TRelated : class
    {
    }

    #endregion
}
