using System.Collections;
using System.Reflection;

namespace FinWise.Shared.Core.Telemetry;

public readonly record struct TokenUsageResult(
    int? ExactInputTokens,
    int? ExactOutputTokens,
    int? ExactTotalTokens,
    string Source,
    bool IsExactAvailable)
{
    public static TokenUsageResult Unavailable => new(null, null, null, "none", false);
}

public static class TokenUsageExtractor
{
    public static TokenUsageResult Extract(object? root)
    {
        if (root is null)
        {
            return TokenUsageResult.Unavailable;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryExtractFromObject(root, visited, depth: 0, out var result) ? result : TokenUsageResult.Unavailable;
    }

    private static bool TryExtractFromObject(
        object? node,
        HashSet<object> visited,
        int depth,
        out TokenUsageResult result)
    {
        result = TokenUsageResult.Unavailable;

        if (node is null || depth > 6)
        {
            return false;
        }

        var type = node.GetType();
        if (IsTerminal(type))
        {
            return false;
        }

        if (!type.IsValueType)
        {
            if (!visited.Add(node))
            {
                return false;
            }
        }

        if (TryExtractUsageFields(node, type, out result))
        {
            return true;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue;
            }

            if (value is null)
            {
                continue;
            }

            if (TryExtractFromObject(value, visited, depth + 1, out result))
            {
                return true;
            }

            if (value is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                {
                    if (TryExtractFromObject(entry.Value, visited, depth + 1, out result))
                    {
                        return true;
                    }
                }
            }
            else if (value is IEnumerable enumerable && value is not string)
            {
                int scanned = 0;
                foreach (var item in enumerable)
                {
                    if (scanned++ > 10)
                    {
                        break;
                    }

                    if (TryExtractFromObject(item, visited, depth + 1, out result))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryExtractUsageFields(object node, Type type, out TokenUsageResult result)
    {
        result = TokenUsageResult.Unavailable;

        int? input = ReadIntProperty(type, node, "InputTokenCount", "PromptTokenCount", "PromptTokens", "InputTokens");
        int? output = ReadIntProperty(type, node, "OutputTokenCount", "CompletionTokenCount", "CompletionTokens", "OutputTokens");
        int? total = ReadIntProperty(type, node, "TotalTokenCount", "TotalTokens");

        if (total is null && input.HasValue && output.HasValue)
        {
            total = input.Value + output.Value;
        }

        if (input.HasValue || output.HasValue || total.HasValue)
        {
            result = new TokenUsageResult(input, output, total, type.FullName ?? type.Name, true);
            return true;
        }

        return false;
    }

    private static int? ReadIntProperty(Type type, object node, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || prop.GetIndexParameters().Length != 0 || !prop.CanRead)
            {
                continue;
            }

            object? value;
            try
            {
                value = prop.GetValue(node);
            }
            catch
            {
                continue;
            }

            if (value is null)
            {
                continue;
            }

            switch (value)
            {
                case int i:
                    return i;
                case long l when l <= int.MaxValue && l >= int.MinValue:
                    return (int)l;
                case short s:
                    return s;
                case byte b:
                    return b;
                default:
                    if (int.TryParse(value.ToString(), out var parsed))
                    {
                        return parsed;
                    }
                    break;
            }
        }

        return null;
    }

    private static bool IsTerminal(Type type)
    {
        return type.IsPrimitive || type.IsEnum
            ? true
            : type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
