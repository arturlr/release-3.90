using System.ComponentModel;
using System.Globalization;

namespace Nop.Core.ComponentModel;

/// <summary>
/// Converts semicolon-separated "key,value" pairs to/from Dictionary&lt;K,V&gt;. Used by settings system.
/// </summary>
public class GenericDictionaryTypeConverter<K, V> : TypeConverter where K : notnull
{
    private readonly TypeConverter _keyConverter = TypeDescriptor.GetConverter(typeof(K))
        ?? throw new InvalidOperationException($"No type converter exists for type {typeof(K).FullName}");
    private readonly TypeConverter _valueConverter = TypeDescriptor.GetConverter(typeof(V))
        ?? throw new InvalidOperationException($"No type converter exists for type {typeof(V).FullName}");

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string input)
        {
            var items = string.IsNullOrEmpty(input)
                ? []
                : input.Split(';', StringSplitOptions.TrimEntries);

            var result = new Dictionary<K, V>();
            foreach (var s in items)
            {
                var kv = string.IsNullOrEmpty(s)
                    ? []
                    : s.Split(',', StringSplitOptions.TrimEntries);
                if (kv.Length == 2)
                {
                    var key = (K?)_keyConverter.ConvertFromInvariantString(kv[0]);
                    var val = (V?)_valueConverter.ConvertFromInvariantString(kv[1]);
                    if (key is not null && val is not null)
                        result.TryAdd(key, val);
                }
            }
            return result;
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IDictionary<K, V> dict)
        {
            return string.Join(';', dict.Select(kv =>
                $"{Convert.ToString(kv.Key, CultureInfo.InvariantCulture)},{Convert.ToString(kv.Value, CultureInfo.InvariantCulture)}"));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
