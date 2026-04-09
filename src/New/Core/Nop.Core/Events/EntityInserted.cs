namespace Nop.Core.Events;

/// <summary>
/// A container for entities that have been inserted.
/// </summary>
public class EntityInserted<T> where T : BaseEntity
{
    public EntityInserted(T entity)
    {
        Entity = entity;
    }

    public T Entity { get; }
}
