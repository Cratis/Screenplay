// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates a document's sample data against what it declares: each pack's rows against the read model their shape supplies
/// rows for, and each <c>ui profile</c>'s sample data selection against the packs and read models.
/// </summary>
internal static class SampleDataValidator
{
    /// <summary>
    /// Validates the sample data of a document.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="slices">Every slice in the document, which declare the read models.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices, ParserContext context)
    {
        var packs = application.SampleDataPacks.ToList();
        var profiles = (application.UiProfiles ?? []).Where(profile => profile.SampleData is not null).ToList();
        if (packs.Count == 0 && profiles.Count == 0)
        {
            return;
        }

        var readModels = new Dictionary<string, ReadModelSyntax>(StringComparer.Ordinal);
        foreach (var readModel in slices.SelectMany(slice => slice.ReadModels ?? []))
        {
            readModels.TryAdd(readModel.Name, readModel);
        }

        var mappings = profiles.SelectMany(profile => profile.SampleData!.Mappings).ToList();
        foreach (var pack in packs)
        {
            foreach (var set in pack.Sets)
            {
                ValidateSet(application, pack, set, readModels, mappings, context);
            }
        }

        foreach (var selection in profiles.Select(profile => profile.SampleData!))
        {
            ValidateSelection(selection, packs, readModels, context);
        }
    }

    static void ValidateSet(
        ApplicationSyntax application,
        SampleDataPackSyntax pack,
        SampleDataSetSyntax set,
        Dictionary<string, ReadModelSyntax> readModels,
        List<SampleDataMappingSyntax> mappings,
        ParserContext context)
    {
        var targets = new List<ReadModelSyntax>();
        if (readModels.TryGetValue(set.Shape, out var byName))
        {
            targets.Add(byName);
        }

        foreach (var mapping in mappings.Where(mapping => mapping.Shape == set.Shape))
        {
            if (readModels.TryGetValue(mapping.ReadModel, out var mapped) && !targets.Contains(mapped))
            {
                targets.Add(mapped);
            }
        }

        if (targets.Count == 0)
        {
            context.Warning(
                DiagnosticCodes.UnknownSampleDataShape,
                $"Shape '{set.Shape}' of sample data pack '{pack.Name}' names no read model - declare a read model '{set.Shape}' or map the shape to one with 'shape {set.Shape} maps to <ReadModel>' in a ui profile's sample data",
                set.Location);
            return;
        }

        foreach (var target in targets)
        {
            foreach (var field in set.Rows.SelectMany(row => row.Fields))
            {
                ValidateField(application, pack, set, target, field, context);
            }
        }
    }

    static void ValidateField(ApplicationSyntax application, SampleDataPackSyntax pack, SampleDataSetSyntax set, ReadModelSyntax target, SampleDataFieldSyntax field, ParserContext context)
    {
        var property = target.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, field.Name, StringComparison.Ordinal));
        if (property is null)
        {
            context.Error(
                DiagnosticCodes.UnknownSampleDataField,
                $"Field '{field.Name}' of shape '{set.Shape}' in sample data pack '{pack.Name}' is not a property of read model '{target.Name}'",
                field.Location);
            return;
        }

        var expected = ExpectedKind(application, property.Type, out var members);
        if (expected is null)
        {
            return;
        }

        string? problem = null;
        if (field.Kind == SampleDataValueKind.Null)
        {
            problem = property.Type.IsOptional ? null : "null is only allowed for an optional property";
        }
        else if (property.Type.IsCollection)
        {
            problem = "a row holds a single value, not a collection";
        }
        else if (expected == SampleDataLiteralType.Unsupported)
        {
            problem = "a row holds a literal, not a structured value";
        }
        else if (!Fits(expected.Value, field.Kind, field.Value, members))
        {
            problem = members is { Count: > 0 }
                ? $"expected one of {string.Join(", ", members.Select(member => $"\"{member}\""))}"
                : $"expected {Describe(expected.Value)}";
        }

        if (problem is not null)
        {
            context.Error(
                DiagnosticCodes.SampleDataTypeMismatch,
                $"Field '{field.Name}' of shape '{set.Shape}' in sample data pack '{pack.Name}' does not fit '{property.Type.Name}' on read model '{target.Name}' - {problem}",
                field.Location);
        }
    }

    static void ValidateSelection(SampleDataSelectionSyntax selection, List<SampleDataPackSyntax> packs, Dictionary<string, ReadModelSyntax> readModels, ParserContext context)
    {
        void CheckPack(SampleDataPackReferenceSyntax? reference)
        {
            if (reference is { IsBlueprint: false } && !packs.Exists(pack => pack.Name == reference.Name))
            {
                context.Error(
                    DiagnosticCodes.UnknownSampleDataPack,
                    $"Unknown sample data pack '{reference.Name}' - declare it with 'sample data {reference.Name}' or use 'blueprint <id>' for a pack a blueprint ships",
                    reference.Location);
            }
        }

        CheckPack(selection.Active);
        foreach (var mapping in selection.Mappings.Where(mapping => !readModels.ContainsKey(mapping.ReadModel)))
        {
            context.Error(DiagnosticCodes.UnknownSampleDataReadModel, $"Unknown read model '{mapping.ReadModel}' in the mapping of shape '{mapping.Shape}' - nothing in the document declares it", mapping.Location);
        }

        foreach (var binding in selection.Bindings)
        {
            CheckPack(binding.Pack);
            if (!readModels.ContainsKey(binding.ReadModel))
            {
                context.Error(DiagnosticCodes.UnknownSampleDataReadModel, $"Unknown read model '{binding.ReadModel}' in a sample data binding - nothing in the document declares it", binding.Location);
            }
        }
    }

    static SampleDataLiteralType? ExpectedKind(ApplicationSyntax application, TypeRefSyntax type, out IReadOnlyList<string>? members)
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
}
