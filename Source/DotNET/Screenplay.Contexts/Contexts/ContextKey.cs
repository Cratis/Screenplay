// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Contexts;

internal static partial class ContextKey
{
    const string MalformedUuid = "the key is not a UUID in N, D, B or P form.";
    const string UnsupportedType = "it is not string, System.Guid or a type with exactly one public constructor taking a single string or System.Guid.";

    internal static T As<T>(string key)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)key;
        }

        if (typeof(T) == typeof(Guid))
        {
            return (T)(object)Uuid(key, typeof(T));
        }

        var constructor = ConstructorFor<T>.Constructor ?? throw new ContextKeyNotConvertible(typeof(T), UnsupportedType);

        var parameterType = ConstructorFor<T>.ParameterType;
        var value = parameterType == typeof(string) ? (object)key : Uuid(key, typeof(T));
        return (T)constructor.Invoke(BindingFlags.DoNotWrapExceptions, null, [value], null);
    }

    static Guid Uuid(string key, Type requestedType)
    {
        if (UuidRegex().IsMatch(key) && Guid.TryParse(key, out var uuid))
        {
            return uuid;
        }

        throw new ContextKeyNotConvertible(requestedType, MalformedUuid);
    }

    [GeneratedRegex(@"^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}|\{[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\}|\([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\))\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UuidRegex();

    static class ConstructorFor<T>
    {
        internal static readonly ConstructorInfo? Constructor = Find();
        internal static readonly Type? ParameterType = Constructor?.GetParameters()[0].ParameterType;

        static ConstructorInfo? Find()
        {
            if (typeof(T).IsAbstract)
            {
                return null;
            }

            var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(constructor => constructor.GetParameters() is [var parameter] && (parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(Guid)))
                .ToArray();
            return constructors.Length == 1 ? constructors[0] : null;
        }
    }
}
