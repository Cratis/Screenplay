// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates sample fields against their declared properties, recursively for composite collection rows.
/// </summary>
internal sealed class SampleDataFieldValidator(
    ApplicationSyntax application,
    SampleDataPackSyntax pack,
    SampleDataSetSyntax set,
    ParserContext context)
{
    /// <summary>
    /// Validates a field against the properties of its row's declared type.
    /// </summary>
    /// <param name="target">The description of the type that owns the row.</param>
    /// <param name="properties">The declared properties of that type.</param>
    /// <param name="field">The authored field to validate.</param>
    public void Validate(string target, IEnumerable<PropertySyntax> properties, SampleDataFieldSyntax field)
    {
        var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Name, field.Name, StringComparison.Ordinal));
        if (property is null)
        {
            context.Error(
                DiagnosticCodes.UnknownSampleDataField,
                $"Field '{field.Name}' of shape '{set.Shape}' in sample data pack '{pack.Name}' is not a property of {target}",
                field.Location);
            return;
        }

        if (field.Kind == SampleDataValueKind.Rows)
        {
            ValidateRows(target, property.Type, field);
            return;
        }

        var expected = ExpectedKind(property.Type, out var members);
        if (expected is null)
        {
            return;
        }

        var problem = LiteralProblem(property.Type, field, expected.Value, members);
        if (problem is not null)
        {
            Mismatch(target, property.Type, field, problem);
        }
    }

    static string? LiteralProblem(TypeRefSyntax type, SampleDataFieldSyntax field, SampleDataLiteralType expected, IReadOnlyList<string>? members)
    {
        if (field.Kind == SampleDataValueKind.Null)
        {
            return type.IsOptional ? null : "null is only allowed for an optional property";
        }

        if (type.IsCollection) return "a row holds a single value, not a collection";
        if (expected == SampleDataLiteralType.Unsupported) return "a row holds a literal, not a structured value";
        if (Fits(expected, field.Kind, field.Value, members)) return null;

        return members is { Count: > 0 }
            ? $"expected one of {string.Join(", ", members.Select(member => $"\"{member}\""))}"
            : $"expected {Describe(expected)}";
    }

    static bool Fits(SampleDataLiteralType expected, SampleDataValueKind kind, string? value, IReadOnlyList<string>? members) => expected switch
    {
        SampleDataLiteralType.Text => kind == SampleDataValueKind.Text && (members?.Contains(value ?? string.Empty, StringComparer.Ordinal) ?? true),
        SampleDataLiteralType.WholeNumber => kind == SampleDataValueKind.Number && value?.All(character => char.IsAsciiDigit(character) || character == '-') == true,
        SampleDataLiteralType.Number => kind == SampleDataValueKind.Number,
        SampleDataLiteralType.Flag => kind == SampleDataValueKind.Flag,
        _ => false
    };

    static string Describe(SampleDataLiteralType expected) => expected switch
    {
        SampleDataLiteralType.Text => "a string",
        SampleDataLiteralType.WholeNumber => "a whole number",
        SampleDataLiteralType.Number => "a number",
        SampleDataLiteralType.Flag => "true or false",
        _ => "a literal"
    };

    void ValidateRows(string target, TypeRefSyntax type, SampleDataFieldSyntax field)
    {
        var composite = (application.Types ?? []).FirstOrDefault(candidate => candidate.Name == type.Name);
        if (!type.IsCollection || composite is null)
        {
            Mismatch(target, type, field, "nested rows need a collection of a composite type");
            return;
        }

        foreach (var child in field.Rows.SelectMany(row => row.Fields))
        {
            Validate($"type '{type.Name}'", composite.Properties, child);
        }
    }

    void Mismatch(string target, TypeRefSyntax type, SampleDataFieldSyntax field, string problem) =>
        context.Error(
            DiagnosticCodes.SampleDataTypeMismatch,
            $"Field '{field.Name}' of shape '{set.Shape}' in sample data pack '{pack.Name}' does not fit '{type.Name}' on {target} - {problem}",
            field.Location);

    SampleDataLiteralType? ExpectedKind(TypeRefSyntax type, out IReadOnlyList<string>? members)
    {
        members = null;
        var concept = application.Concepts.FirstOrDefault(candidate => candidate.Name == type.Name);
        if (concept is not null)
        {
            var values = concept.Values.ToList();
            if (values.Count > 0)
            {
                members = values;
                return SampleDataLiteralType.Text;
            }

            return SampleDataLiteralTypes.Of(concept.Type);
        }

        if ((application.Types ?? []).Any(candidate => candidate.Name == type.Name))
        {
            return SampleDataLiteralType.Unsupported;
        }

        return SampleDataLiteralTypes.Of(type.Name);
    }
}
