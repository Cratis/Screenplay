// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Gives syntax read from a contract the location of the line that names the extension, so a model that uses it binds and reports
/// against its own source document rather than against a file the compiler does not own.
/// </summary>
static class SourceRelocator
{
    static readonly MethodInfo _clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static T Relocate<T>(T value, SourceLocation to) => (T)Relocate(value, typeof(T), to)!;

    static object? Relocate(object? value, Type declared, SourceLocation to)
    {
        if (value is null) return null;
        if (value is SourceLocation) return to;
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string || (type.IsValueType && !IsImmutableArray(type))) return value;
        var map = type.GetInterfaces().Append(type).FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
        if (map is not null && value is IEnumerable entries)
        {
            var arguments = map.GetGenericArguments();
            var (keyType, valueType) = (arguments[0], arguments[1]);
            var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
            foreach (var entry in entries)
            {
                var pair = entry.GetType();
                dictionary.Add(pair.GetProperty("Key")!.GetValue(entry)!, Relocate(pair.GetProperty("Value")!.GetValue(entry), valueType, to));
            }

            return dictionary;
        }

        if (value is IEnumerable sequence)
        {
            var element = ElementType(declared) ?? ElementType(type) ?? typeof(object);
            var items = sequence.Cast<object?>().Select(item => Relocate(item, element, to)).ToList();
            var array = Array.CreateInstance(element, items.Count);
            for (var index = 0; index < items.Count; index++) array.SetValue(items[index], index);
            if (declared.IsGenericType && declared.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                return typeof(ImmutableArray).GetMethods().First(method => method.Name == nameof(ImmutableArray.Create) && method.GetParameters() is [{ ParameterType.IsArray: true }])
                    .MakeGenericMethod(element).Invoke(null, [array]);
            }

            return array;
        }

        var copy = _clone.Invoke(value, null)!;
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.GetIndexParameters().Length == 0 && property.SetMethod is not null && property.GetMethod is not null))
        {
            var current = property.GetValue(value);
            if (current is null) continue;
            property.SetValue(copy, Relocate(current, property.PropertyType, to));
        }

        return copy;
    }

    static bool IsImmutableArray(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>);

    static Type? ElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }
}
