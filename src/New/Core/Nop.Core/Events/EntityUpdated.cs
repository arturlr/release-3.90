namespace Nop.Core.Events;

/// <summary>
/// A container for entities that have been updated.
/// </summary>
public class EntityUpdated<T> where T : BaseEntity
{
    public EntityUpdated(T entity)
    {
        Entity = entity;
    }

    public T Entity { get; }
}
