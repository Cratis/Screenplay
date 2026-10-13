// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceGenerationRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is PropertySyntax { IsGenerated: true }).ToArray();
        if (subjects.Length != 1 || index.Find(subjects[0].Parent!) is not { Node: CommandSyntax command } owner ||
            index.Find(owner.Parent!) is not { Node: SliceSyntax slice } sliceEntry)
        {
            return [];
        }
        var subject = subjects[0];
        var property = (PropertySyntax)subject.Node;
        var concepts = index.Entries.Where(entry => entry.Node is ConceptSyntax concept && concept.Name == property.Type.Name).ToArray();
        if (concepts.Length != 1 || concepts[0].Node is not ConceptSyntax { Generation: { } generation } concept ||
            GenerationEntropy.Bits(generation) is not { } bits || bits >= 64)
        {
            return [];
        }
        var repairs = ImmutableArray.CreateBuilder<WorkspaceDiagnosticRepair>();
        var replacement = Raise(generation);
        if (replacement is not null)
        {
            var entry = concepts[0];
            var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, entry.Handle, [new ReplaceWorkspaceNode(entry.Handle, concept, concept with { Generation = replacement })])
            {
                Title = "Raise generation to at least 64 random bits",
                CanFixAll = false
            };
            repairs.AddRange(WorkspaceRepairVerification.Discover(index, repair, verify));
        }
        if (diagnostic.Code == DiagnosticCodes.LowGenerationEntropy && Unique(index, command, property, slice) is { } constraint)
        {
            var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new AddWorkspaceNode(sliceEntry.Handle, slice, "constraints", constraint)])
            {
                Title = "Add a unique constraint for the generated property",
                CanFixAll = false
            };
            repairs.AddRange(WorkspaceRepairVerification.Discover(index, repair, verify));
        }

        return repairs.ToImmutable();
    }

    static GenerationSyntax? Raise(GenerationSyntax generation)
    {
        if (generation.Strategy == "bytes") return generation with { Count = 8 };
        if (generation.Strategy != "nanoid") return null;
        var size = Enumerable.Range(1, 256).FirstOrDefault(size => GenerationEntropy.Bits(generation with { Size = size }) >= 64);

        return size == 0 ? null : generation with { Size = size };
    }

    static UniquePropertyConstraintSyntax? Unique(WorkspaceSyntaxIndex index, CommandSyntax command, PropertySyntax property, SliceSyntax slice)
    {
        var mappings = command.Produces.Where(production => index.Productions.IsEventProduction(production, slice))
            .SelectMany(production => production.Mappings.Where(mapping => mapping.Source is PathExpressionSyntax path && path.Path == property.Name)
                .Select(mapping => (production.Event, mapping.Property))).ToArray();
        if (mappings.Length != 1 || mappings[0].Property.Contains('.', StringComparison.Ordinal)) return null;
        var name = $"Unique{command.Name}{property.Name}";
        if (index.Entries.Any(entry => entry.Node is ConstraintSyntax constraint && constraint.Name == name)) return null;

        return new(name, mappings[0].Property, mappings[0].Event, property.Location);
    }
}
