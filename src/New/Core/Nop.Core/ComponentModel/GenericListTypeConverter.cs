using System.ComponentModel;
using System.Globalization;

namespace Nop.Core.ComponentModel;

/// <summary>
/// Converts comma-separated string to/from List&lt;T&gt;. Used by settings system.
/// </summary>
public class GenericListTypeConverter<T> : TypeConverter
{
    private readonly TypeConverter _typeConverter = TypeDescriptor.GetConverter(typeof(T))
        ?? throw new InvalidOperationException($"No type converter exists for type {typeof(T).FullName}");

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string input)
        {
            var items = string.IsNullOrEmpty(input)
                ? []
                : input.Split(',', StringSplitOptions.TrimEntries);

            var result = new List<T>();
            foreach (var s in items)
            {
                var item = _typeConverter.ConvertFromInvariantString(s);
                if (item is not null)
                    result.Add((T)item);
            }
            return result;
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IList<T> list)
            return string.Join(',', list.Select(i => Convert.ToString(i, CultureInfo.InvariantCulture)));

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
