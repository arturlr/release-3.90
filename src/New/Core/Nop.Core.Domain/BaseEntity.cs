namespace Nop.Core;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id == default || other.Id == default)
            return false;

        if (Id != other.Id)
            return false;

        var otherType = other.GetType();
        var thisType = GetType();
        return thisType.IsAssignableFrom(otherType) || otherType.IsAssignableFrom(thisType);
    }

    public override int GetHashCode()
    {
        return Id == default ? base.GetHashCode() : Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? x, BaseEntity? y)
    {
        return Equals(x, y);
    }

    public static bool operator !=(BaseEntity? x, BaseEntity? y)
    {
        return !Equals(x, y);
    }
}
