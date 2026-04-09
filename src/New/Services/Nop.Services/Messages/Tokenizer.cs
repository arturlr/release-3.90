using System.Net;
using System.Text.RegularExpressions;
using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public partial class Tokenizer(MessageTemplatesSettings messageTemplatesSettings) : ITokenizer
{
    public string Replace(string template, IEnumerable<Token> tokens, bool htmlEncode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(tokens);

        // resolve conditional statements first
        template = ReplaceConditionalStatements(template, tokens);

        // then replace tokens
        template = ReplaceTokens(template, tokens, htmlEncode);

        return template;
    }

    private string ReplaceTokens(string template, IEnumerable<Token> tokens, bool htmlEncode = false, bool stringWithQuotes = false)
    {
        foreach (var token in tokens)
        {
            var tokenValue = token.Value ?? string.Empty;

            if (stringWithQuotes && tokenValue is string)
                tokenValue = $"\"{tokenValue}\"";
            else if (htmlEncode && !token.NeverHtmlEncoded)
                tokenValue = WebUtility.HtmlEncode(tokenValue.ToString() ?? string.Empty);

            template = ReplaceString(template, $"%{token.Key}%", tokenValue.ToString()!);
        }

        return template;
    }

    private string ReplaceString(string original, string pattern, string replacement)
    {
        var comparison = messageTemplatesSettings.CaseInvariantReplacement
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (comparison == StringComparison.Ordinal)
            return original.Replace(pattern, replacement);

        return original.Replace(pattern, replacement, comparison);
    }

    private string ReplaceConditionalStatements(string template, IEnumerable<Token> tokens)
    {
        var regexFull = FullConditionalRegex();
        var regexCondition = ConditionRegex();

        var conditionalStatements = regexFull.Matches(template)
            .SelectMany(match => match.Groups["Condition"].Captures.Select(capture => new
            {
                capture.Index,
                FullStatement = capture.Value,
                Condition = regexCondition.Match(capture.Value).Value
            }))
            .ToList();

        if (conditionalStatements.Count == 0)
            return template;

        foreach (var statement in conditionalStatements.OrderBy(s => s.Index))
        {
            var conditionIsMet = false;
            if (!string.IsNullOrEmpty(statement.Condition))
            {
                try
                {
                    // replace tokens in condition (string values wrapped in quotes for evaluation)
                    var conditionString = ReplaceTokens(statement.Condition, tokens, stringWithQuotes: true);
                    // simple evaluation: check if the condition string contains a non-empty/non-false value
                    conditionIsMet = EvaluateCondition(conditionString);
                }
                catch
                {
                    // condition evaluation failed — treat as false
                }
            }

            template = template.Replace(conditionIsMet ? statement.Condition : statement.FullStatement, string.Empty);
        }

        template = template.Replace("%if", string.Empty).Replace("endif%", string.Empty);
        return template;
    }

    private static bool EvaluateCondition(string condition)
    {
        // strip outer parentheses and whitespace
        condition = condition.Trim();
        if (condition.StartsWith('(') && condition.EndsWith(')'))
            condition = condition[1..^1].Trim();

        // empty condition = false
        if (string.IsNullOrWhiteSpace(condition))
            return false;

        // check for simple equality: "value1" == "value2"
        var eqParts = condition.Split("==", 2, StringSplitOptions.TrimEntries);
        if (eqParts.Length == 2)
            return string.Equals(Unquote(eqParts[0]), Unquote(eqParts[1]), StringComparison.OrdinalIgnoreCase);

        // check for inequality: "value1" != "value2"
        var neqParts = condition.Split("!=", 2, StringSplitOptions.TrimEntries);
        if (neqParts.Length == 2)
            return !string.Equals(Unquote(neqParts[0]), Unquote(neqParts[1]), StringComparison.OrdinalIgnoreCase);

        // single value: non-empty, non-"false", non-"0" = true
        var val = Unquote(condition);
        return !string.IsNullOrWhiteSpace(val) &&
               !val.Equals("false", StringComparison.OrdinalIgnoreCase) &&
               val != "0";
    }

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s[1..^1];
        return s;
    }

    [GeneratedRegex(@"(?:(?'Group' %if)|(?'Condition-Group' endif%)|(?! (%if|endif%)).)*(?(Group)(?!))",
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FullConditionalRegex();

    [GeneratedRegex(@"\s*\((?:(?'Group' \()|(?'-Group' \))|[^()])*(?(Group)(?!))\)\s*",
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ConditionRegex();
}
