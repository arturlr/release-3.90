namespace Nop.Core;

/// <summary>
/// Represents errors that occur during application execution
/// </summary>
public class NopException : Exception
{
    public NopException()
    {
    }

    public NopException(string message) : base(message)
    {
    }

    public NopException(string messageFormat, params object[] args)
        : base(string.Format(messageFormat, args))
    {
    }

    public NopException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
