// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxJsonWriter
{
    internal static Dictionary<string, object?> Write(SyntaxNode node, string path, int depth)
    {
        SyntaxJson.CheckDepth(depth, path);
        var descriptor = SyntaxKinds.For(node.GetType());
        var result = new Dictionary<string, object?>(StringComparer.Ordinal) { ["kind"] = descriptor.Type.Name };
        foreach (var member in descriptor.Members)
        {
            result.Add(member.Name, WriteMember(member, member.Property.GetValue(node), $"{path}.{member.Name}", depth + 1));
        }

        return result;
    }

    static object? WriteMember(SyntaxMember member, object? value, string path, int depth)
    {
        if (value is null)
        {
            if (!member.Nullable)
            {
                throw new InvalidSyntaxJson($"{path}: null is not permitted.");
            }

            return member.ElementType is not null ? Array.Empty<object>() : null;
        }

        if (member.ElementType is not null)
        {
            return ((IEnumerable)value).Cast<object?>()
                .Select((item, index) => WriteValue(item, member.ElementType, $"{path}[{index}]", depth))
                .ToArray();
        }

        return WriteValue(value, member.Type, path, depth);
    }

    static object WriteValue(object? value, Type type, string path, int depth)
    {
        if (value is null)
        {
            throw new InvalidSyntaxJson($"{path}: collection items cannot be null.");
        }

        return value is SyntaxNode node ? Write(node, path, depth) : SyntaxScalars.Write(value, type, path);
    }
}
