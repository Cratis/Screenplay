// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson.given;

internal static class syntax_examples
{
    internal static IEnumerable<Type> Types => typeof(SyntaxNode).Assembly.GetTypes()
        .Where(type => type.IsPublic && !type.IsAbstract && typeof(SyntaxNode).IsAssignableFrom(type));

    // Independent of the codec descriptors: omission of a kind or init member must not hide from the specs.
    internal static SyntaxNode Create(Type type, int depth = 0)
    {
        if (type.IsAbstract)
        {
            type = Types.Where(type.IsAssignableFrom).OrderBy(candidate => candidate.GetConstructors()[0].GetParameters().Length).First();
        }

        var constructor = type.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        var parameters = constructor.GetParameters();
        var node = (SyntaxNode)constructor.Invoke([.. parameters.Select(parameter => Value(parameter.ParameterType, depth + 1))]);
        foreach (var property in type.GetProperties().Where(property => property.SetMethod?.IsPublic == true &&
            !parameters.Any(parameter => parameter.Name == property.Name) && property.Name is not "DescriptionLocation" and not "DescriptionRawLength" &&
            !property.IsDefined(typeof(SourceSpanMetadataAttribute), true)))
        {
            property.SetValue(node, Value(property.PropertyType, depth + 1));
        }

        return node;
    }

    internal static bool SameValues(object? left, object? right)
    {
        if (left is SyntaxNode node && right is SyntaxNode other)
        {
            return node.GetType() == other.GetType() && node.GetType().GetProperties()
                .Where(property => property.SetMethod?.IsPublic == true && property.PropertyType != typeof(SourceLocation) &&
                    property.Name is not "DescriptionRawLength" && !property.IsDefined(typeof(SourceSpanMetadataAttribute), true))
                .All(property => SameValues(property.GetValue(node), property.GetValue(other)));
        }

        if (left is IEnumerable sequence and not string && right is IEnumerable otherSequence)
        {
            var items = sequence.Cast<object?>().ToArray();
            var otherItems = otherSequence.Cast<object?>().ToArray();

            return items.Length == otherItems.Length && items.Zip(otherItems).All(pair => SameValues(pair.First, pair.Second));
        }

        return Equals(left, right);
    }

    static object? Value(Type type, int depth)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(SourceLocation))
        {
            return new SourceLocation(12, 4, "examples.play");
        }

        if (type == typeof(string) || type == typeof(object))
        {
            return "example";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 42;
        }

        if (type == typeof(TimeOnly))
        {
            return new TimeOnly(12, 34, 56);
        }

        if (type == typeof(ValidationSeverity))
        {
            return ValidationSeverity.Warning;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (typeof(SyntaxNode).IsAssignableFrom(type))
        {
            return Create(type, depth);
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var array = Array.CreateInstance(elementType, depth < 3 ? 1 : 0);
            if (array.Length > 0)
            {
                array.SetValue(Value(elementType, depth + 1), 0);
            }

            return array;
        }

        throw new InvalidSyntaxJson($"The independent syntax example factory does not support {type.Name}.");
    }
}
