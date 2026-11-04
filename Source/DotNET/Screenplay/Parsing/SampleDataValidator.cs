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

        var validator = new SampleDataFieldValidator(application, pack, set, context);
        foreach (var target in targets)
        {
            foreach (var field in set.Rows.SelectMany(row => row.Fields))
            {
                validator.Validate($"read model '{target.Name}'", target.Properties, field);
            }
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
}
