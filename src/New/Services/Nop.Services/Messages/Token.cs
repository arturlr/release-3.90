namespace Nop.Services.Messages;

public sealed class Token(string key, object value, bool neverHtmlEncoded = false)
{
    public string Key { get; } = key;
    public object Value { get; } = value;
    public bool NeverHtmlEncoded { get; } = neverHtmlEncoded;

    public override string ToString() => $"{Key}: {Value}";
}
