using System.Xml;

namespace Nop.Core;

/// <summary>
/// Extension methods
/// </summary>
public static class Extensions
{
    public static bool IsNullOrDefault<T>(this T? value) where T : struct =>
        default(T).Equals(value.GetValueOrDefault());

    public static string ElText(this XmlNode node, string elName) =>
        node.SelectSingleNode(elName)!.InnerText;

    public static TResult Return<TInput, TResult>(this TInput? o, Func<TInput, TResult> evaluator, TResult failureValue)
        where TInput : class =>
        o is null ? failureValue : evaluator(o);
}
