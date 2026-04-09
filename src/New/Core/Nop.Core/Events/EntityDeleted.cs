namespace Nop.Core.Events;

/// <summary>
/// A container for entities that have been deleted (hard delete, not soft-delete via bit column).
/// </summary>
public class EntityDeleted<T> where T : BaseEntity
{
    public EntityDeleted(T entity)
    {
        Entity = entity;
    }

    public T Entity { get; }
}
