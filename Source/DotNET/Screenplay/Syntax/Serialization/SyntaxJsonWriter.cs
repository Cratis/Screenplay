// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxJsonWriter
{
    internal static Dictionary<string, object?> Write(SyntaxNode node, string path, int depth)
    {
        SyntaxJson.CheckDepth(depth, path);
        ImplementationInvariants.Validate(node);
        var descriptor = SyntaxKinds.For(node.GetType());
        var result = new Dictionary<string, object?>(StringComparer.Ordinal) { ["kind"] = descriptor.Type.Name };
        foreach (var member in descriptor.Members)
        {
            var value = member.Property.GetValue(node);
            if (node is ConceptAttributeSyntax && (member.Name == "scope" || member.Name == "specialCategory") && value is null) continue;
            if (node is ConceptAttributeSyntax && member.Name == "criminal" && Equals(value, false)) continue;
            if (member.Name == "documentation" && node is not EventSyntax && value is null) continue;
            if (node is Specifications.SpecificationSyntax && (member.Name == "description" || member.Name == "givenCallerPersona") && value is null) continue;
            if (member.Type == typeof(SourceOptions) && Equals(value, SourceOptions.Legacy)) continue;
            if ((member.Name == "examples" || (node is Specifications.SpecificationSyntax && (member.Name == "parameters" || member.Name == "cases"))) && value is IEnumerable examples && !examples.Cast<object>().Any()) continue;
            if (node is Specifications.SpecificationErrorSyntax && member.Name == "caseValue" && value is null) continue;
            if (member.Name == "inlineProperty" && value is null) continue;
            if (node is Specifications.SpecificationExampleSyntax or Specifications.SpecificationRedeliverySyntax && (member.Name == "stream" || member.Name == "noStream") && value is null) continue;
            if (node is Specifications.SpecificationSyntax && member.Name == "thenNoEvents" && Equals(value, false)) continue;
            if (node is InteractionBindingSyntax binding && member.Name == "alternatives" && !binding.Alternatives.Any()) continue;
            if (node is InteractionBindingSyntax && member.Name == "otherwise" && value is null) continue;
            if (node is InvokesSyntax invocation && member.Name == "onRefused" && !invocation.OnRefused.Any()) continue;
            if (node is Specifications.SpecificationSyntax && member.Name == "whenRedelivered" && value is null) continue;
            if (member.Name == "dependsOn" && member.ElementType == typeof(DependsOnSyntax) && value is IEnumerable<DependsOnSyntax> dependencies && !dependencies.Any()) continue;
            result.Add(member.Name, WriteMember(member, value, $"{path}.{member.Name}", depth + 1));
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
