// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxJsonReader
{
    internal static SyntaxNode Read(JsonElement value, Type expected, string path, int depth)
    {
        SyntaxJson.CheckDepth(depth, path);
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSyntaxJson($"{path}: expected a typed syntax object.");
        }

        var properties = Properties(value, path);
        if (!properties.TryGetValue("kind", out var kind) || kind.ValueKind != JsonValueKind.String)
        {
            throw new InvalidSyntaxJson($"{path}.kind: a string syntax discriminator is required.");
        }

        var descriptor = SyntaxKinds.For(kind.GetString()!);
        if (!expected.IsAssignableFrom(descriptor.Type))
        {
            throw new InvalidSyntaxJson($"{path}: {descriptor.Type.Name} is not a {expected.Name}.");
        }

        var known = descriptor.Members.Select(member => member.Name).Append("kind").ToHashSet(StringComparer.Ordinal);
        foreach (var name in properties.Keys.Where(name => !known.Contains(name)))
        {
            throw new InvalidSyntaxJson($"{path}.{name}: unknown property of {descriptor.Type.Name}.");
        }

        var values = descriptor.Members.ToDictionary(
            member => member.Name,
            member => ReadMember(properties, member, $"{path}.{member.Name}", depth + 1),
            StringComparer.Ordinal);

        return descriptor.Create(values);
    }

    static Dictionary<string, JsonElement> Properties(JsonElement value, string path)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!result.TryAdd(property.Name, property.Value))
            {
                throw new InvalidSyntaxJson($"{path}.{property.Name}: duplicate property.");
            }
        }

        return result;
    }

    static object? ReadMember(Dictionary<string, JsonElement> values, SyntaxMember member, string path, int depth)
    {
        if (!values.TryGetValue(member.Name, out var value))
        {
            if (member.Required)
            {
                throw new InvalidSyntaxJson($"{path}: required property is missing.");
            }

            return member.MissingValue;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            if (!member.Nullable)
            {
                throw new InvalidSyntaxJson($"{path}: null is not permitted.");
            }

            return member.ElementType is not null ? member.MissingValue : null;
        }

        if (member.ElementType is not null)
        {
            return ReadCollection(value, member.ElementType, path, depth);
        }

        return ReadValue(value, member.Type, path, depth);
    }

    static Array ReadCollection(JsonElement value, Type elementType, string path, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidSyntaxJson($"{path}: expected an array.");
        }

        var result = Array.CreateInstance(elementType, value.GetArrayLength());
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            result.SetValue(ReadValue(item, elementType, $"{path}[{index}]", depth), index);
            index++;
        }

        return result;
    }

    static object ReadValue(JsonElement value, Type type, string path, int depth) => typeof(SyntaxNode).IsAssignableFrom(type)
        ? Read(value, type, path, depth)
        : SyntaxScalars.Read(value, type, path);
}
