using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nop.Core;

/// <summary>
/// Common helper methods
/// </summary>
public static partial class CommonHelper
{
    [GeneratedRegex(
        @"^(?:[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+\.)*[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!\.)){0,61}[a-zA-Z0-9]?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\[(?:(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\.){3}(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\]))$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public static string EnsureSubscriberEmailOrThrow(string email)
    {
        var output = EnsureNotNull(email).Trim();
        output = EnsureMaximumLength(output, 255);
        if (!IsValidEmail(output))
            throw new NopException("Email is not valid.");
        return output;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
        return EmailRegex().IsMatch(email.Trim());
    }

    public static bool IsValidIpAddress(string? ipAddress) =>
        IPAddress.TryParse(ipAddress, out _);

    public static string GenerateRandomDigitCode(int length)
    {
        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(result);
    }

    public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue) =>
        RandomNumberGenerator.GetInt32(min, max);

    public static string EnsureMaximumLength(string? str, int maxLength, string? postfix = null)
    {
        if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
            return str ?? string.Empty;

        var pLen = postfix?.Length ?? 0;
        var result = str[..(maxLength - pLen)];
        if (!string.IsNullOrEmpty(postfix))
            result += postfix;
        return result;
    }

    public static string EnsureNumericOnly(string? str) =>
        string.IsNullOrEmpty(str) ? string.Empty : new string(str.Where(char.IsDigit).ToArray());

    public static string EnsureNotNull(string? str) => str ?? string.Empty;

    public static bool AreNullOrEmpty(params string?[] stringsToValidate) =>
        stringsToValidate.Any(string.IsNullOrEmpty);

    public static bool ArraysEqual<T>(T[]? a1, T[]? a2)
    {
        if (ReferenceEquals(a1, a2)) return true;
        if (a1 is null || a2 is null) return false;
        if (a1.Length != a2.Length) return false;
        return a1.AsSpan().SequenceEqual(a2);
    }

    public static void SetProperty(object instance, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(propertyName);

        var pi = instance.GetType().GetProperty(propertyName)
            ?? throw new NopException("No property '{0}' found on type '{1}'.", propertyName, instance.GetType());
        if (!pi.CanWrite)
            throw new NopException("Property '{0}' on type '{1}' has no setter.", propertyName, instance.GetType());
        if (value is not null && !pi.PropertyType.IsInstanceOfType(value))
            value = To(value, pi.PropertyType);
        pi.SetValue(instance, value);
    }

    public static object? To(object? value, Type destinationType) =>
        To(value, destinationType, CultureInfo.InvariantCulture);

    public static object? To(object? value, Type destinationType, CultureInfo culture)
    {
        if (value is null) return null;

        var destinationConverter = TypeDescriptor.GetConverter(destinationType);
        if (destinationConverter.CanConvertFrom(value.GetType()))
            return destinationConverter.ConvertFrom(null, culture, value);

        var sourceConverter = TypeDescriptor.GetConverter(value.GetType());
        if (sourceConverter.CanConvertTo(destinationType))
            return sourceConverter.ConvertTo(null, culture, value, destinationType);

        if (destinationType.IsEnum && value is int intVal)
            return Enum.ToObject(destinationType, intVal);

        if (!destinationType.IsInstanceOfType(value))
            return Convert.ChangeType(value, destinationType, culture);

        return value;
    }

    public static T To<T>(object value) => (T)To(value, typeof(T))!;

    public static string ConvertEnum(string? str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        var result = string.Concat(str.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
        return result.TrimStart();
    }

    public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
    {
        var age = endDate.Year - startDate.Year;
        if (startDate > endDate.AddYears(-age))
            age--;
        return age;
    }
}
