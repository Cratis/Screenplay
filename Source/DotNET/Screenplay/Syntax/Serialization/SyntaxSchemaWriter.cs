// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxSchemaWriter
{
    static readonly string[] _discriminator = ["kind"];

    internal static Dictionary<string, object?> For(SyntaxDescriptor descriptor)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = new Dictionary<string, object?> { ["type"] = "string", ["const"] = descriptor.Type.Name }
        };
        foreach (var member in descriptor.Members)
        {
            properties[member.Name] = ForMember(member);
        }

        return new Dictionary<string, object?>
        {
            ["title"] = descriptor.Type.Name,
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties,
            ["required"] = _discriminator.Concat(descriptor.Members.Where(member => member.Required).Select(member => member.Name)).ToArray()
        };
    }

    static Dictionary<string, object?> ForMember(SyntaxMember member)
    {
        var shape = member.ElementType is not null
            ? new Dictionary<string, object?> { ["type"] = "array", ["items"] = ForType(member.ElementType) }
            : ForType(member.Type);
        if (member.Nullable && member.Type != typeof(object))
        {
            shape = new Dictionary<string, object?> { ["anyOf"] = new[] { shape, Type("null") } };
        }

        if (member.ElementType is not null)
        {
            shape["default"] = Array.Empty<object>();
        }
        else if (!member.Required)
        {
            var value = member.MissingValue;
            shape["default"] = value is null ? null : SyntaxScalars.Write(value, member.Type, member.Name);
        }

        return shape;
    }

    static Dictionary<string, object?> ForType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (typeof(SyntaxNode).IsAssignableFrom(type))
        {
            return new Dictionary<string, object?>
            {
                ["oneOf"] = SyntaxKinds.All.Where(descriptor => type.IsAssignableFrom(descriptor.Type))
                    .Select(descriptor => new Dictionary<string, object?> { ["$ref"] = $"#/$defs/{descriptor.Type.Name}" }).ToArray()
            };
        }

        if (type.IsEnum)
        {
            return new Dictionary<string, object?> { ["type"] = "string", ["enum"] = Enum.GetNames(type) };
        }

        if (type == typeof(TimeOnly))
        {
            return new Dictionary<string, object?> { ["type"] = "string", ["pattern"] = SyntaxScalars.TimePattern };
        }

        if (type == typeof(int))
        {
            return new Dictionary<string, object?> { ["type"] = "integer", ["minimum"] = int.MinValue, ["maximum"] = int.MaxValue };
        }

        if (type == typeof(uint))
        {
            return new Dictionary<string, object?> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = uint.MaxValue };
        }

        if (type == typeof(object))
        {
            return SyntaxLiterals.Schema();
        }

        if (type == typeof(string) || type == typeof(bool))
        {
            return Type(type == typeof(string) ? "string" : "boolean");
        }

        throw new InvalidSyntaxJson($"No syntax schema exists for structural type '{type.Name}'.");
    }

    static Dictionary<string, object?> Type(string name) => new() { ["type"] = name };
}
