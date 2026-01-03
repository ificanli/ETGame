using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ET.Test;

/// <summary>
///     兼容 xUnit 断言 API，内部委托给 MSTest Assert
/// </summary>
public static class Assert
{
    public static void True(bool? condition, string? message = null)
    {
        if (!condition.HasValue)
            throw new MsAssertFailedException(message ?? "期望为 True，但值为 null。");

        MsTestAssert.IsTrue(condition.Value, message);
    }

    public static void False(bool? condition, string? message = null)
    {
        if (!condition.HasValue)
            throw new MsAssertFailedException(message ?? "期望为 False，但值为 null。");

        MsTestAssert.IsFalse(condition.Value, message);
    }

    public static void Equal<T>(T? expected, T? actual)
    {
        MsTestAssert.AreEqual(expected, actual);
    }

    public static void Equal(double expected, double actual, int precision)
    {
        var delta = Math.Pow(10, -precision);
        MsTestAssert.AreEqual(expected, actual, delta, $"期望两数在小数点后 {precision} 位内相等。");
    }

    public static void Equal(decimal expected, decimal actual, int precision)
    {
        var delta = Math.Pow(10, -precision);
        MsTestAssert.AreEqual((double)expected, (double)actual, delta, $"期望两数在小数点后 {precision} 位内相等。");
    }

    public static void NotEqual<T>(T? expected, T? actual)
    {
        MsTestAssert.AreNotEqual(expected, actual);
    }

    public static string Contains(string expectedSubstring, string? actualString)
    {
        return Contains(expectedSubstring, actualString, StringComparison.Ordinal);
    }

    public static string Contains(string expectedSubstring, string? actualString, StringComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(expectedSubstring);
        if (actualString is null)
            throw new MsAssertFailedException("实际字符串为 null。");

        if (!actualString.Contains(expectedSubstring, comparison))
            throw new MsAssertFailedException($"期望 \"{actualString}\" 包含 \"{expectedSubstring}\"。");

        return actualString;
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (!collection.Contains(expected))
            throw new MsAssertFailedException("集合中未找到期望元素。");
    }

    public static T Contains<T>(IEnumerable<T> collection, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in collection)
            if (predicate(item))
                return item;

        throw new MsAssertFailedException("集合中未找到满足条件的元素。");
    }

    public static string DoesNotContain(string substring, string? actualString)
    {
        return DoesNotContain(substring, actualString, StringComparison.Ordinal);
    }

    public static string DoesNotContain(string substring, string? actualString, StringComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(substring);
        if (actualString is null)
            throw new MsAssertFailedException("实际字符串为 null。");

        if (actualString.Contains(substring, comparison))
            throw new MsAssertFailedException($"不应包含 \"{substring}\"，但实际为 \"{actualString}\"。");

        return actualString;
    }

    public static void Empty(IEnumerable collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection.Cast<object>().Any())
            throw new MsAssertFailedException("集合应为空。");
    }

    public static void Empty(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 0)
            throw new MsAssertFailedException("字符串应为空。");
    }

    public static IEnumerable NotEmpty(IEnumerable collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (!collection.Cast<object>().Any())
            throw new MsAssertFailedException("集合不应为空。");

        return collection;
    }

    public static string NotEmpty(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
            throw new MsAssertFailedException("字符串不应为空。");

        return value;
    }

    public static void NotNull(object? value)
    {
        MsTestAssert.IsNotNull(value);
    }

    public static void Null(object? value)
    {
        MsTestAssert.IsNull(value);
    }

    public static T Single<T>(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        using var enumerator = collection.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new MsAssertFailedException("集合应包含一个元素，但实际为空。");

        var result = enumerator.Current;
        if (enumerator.MoveNext())
            throw new MsAssertFailedException("集合应只包含一个元素，但实际包含多个。");

        return result;
    }

    public static object Single(IEnumerable collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        var enumerator = collection.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                throw new MsAssertFailedException("集合应包含一个元素，但实际为空。");

            var result = enumerator.Current;
            if (enumerator.MoveNext())
                throw new MsAssertFailedException("集合应只包含一个元素，但实际包含多个。");

            return result!;
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    public static void NotEmpty<T>(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (!collection.Any())
            throw new MsAssertFailedException("集合不应为空。");
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(action);
        MsTestAssert.ThrowsException<TException>(action);
    }

    public static Task<TException> ThrowsAsync<TException>(Func<Task> testCode) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(testCode);
        return MsTestAssert.ThrowsExceptionAsync<TException>(testCode);
    }

    public static Task<TException> ThrowsAnyAsync<TException>(Func<Task> testCode) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(testCode);
        return MsTestAssert.ThrowsExceptionAsync<TException>(testCode);
    }
}
