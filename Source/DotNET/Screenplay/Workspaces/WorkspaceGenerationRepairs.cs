// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceGenerationRepairs
{
    /// <summary>
    /// Determines whether a diagnostic code is repaired by generation repairs.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>True when the code is a low generation or generated identifier entropy diagnostic.</returns>
    internal static bool Handles(string code) => code == DiagnosticCodes.LowGenerationEntropy || code == DiagnosticCodes.LowGeneratedIdentifierEntropy;

    /// <summary>
    /// Finds the generation repairs for one subject.
    /// </summary>
    /// <param name="index">The syntax index.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="subject">The repair subject.</param>
    /// <param name="find">Finds the repairs of one diagnostic.</param>
    /// <returns>The repairs for the subject, or null when the code is not a generation diagnostic.</returns>
    internal static IEnumerable<WorkspaceDiagnosticRepair>? ForSubject(WorkspaceSyntaxIndex index, string code, WorkspaceNodeHandle subject, Func<Diagnostic, IEnumerable<WorkspaceDiagnosticRepair>> find)
    {
        if (!Handles(code))
        {
            return null;
        }

        // Length repairs target the concept, which can be in another document than the diagnostic.
        // Distinct subjects also select the length and unique-constraint actions without a new tool argument.
        return index.RepairableDiagnostics.Where(diagnostic => diagnostic.Code == code)
            .SelectMany(find)
            .Where(repair => repair.Subject == subject)
            .DistinctBy(repair => repair.Title);
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        if (GeneratedProperty(index, revision, diagnostic) is not { } subject ||
            index.Find(subject.Parent!) is not { Node: CommandSyntax command } owner ||
            index.Find(owner.Parent!) is not { Node: SliceSyntax slice } sliceEntry ||
            LowEntropyConcept(index, (PropertySyntax)subject.Node) is not { } conceptEntry)
        {
            return [];
        }

        var property = (PropertySyntax)subject.Node;
        var concept = (ConceptSyntax)conceptEntry.Node;
        var repairs = ImmutableArray.CreateBuilder<WorkspaceDiagnosticRepair>();
        if (Raise(concept.Generation!) is { } replacement)
        {
            var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, conceptEntry.Handle, [new ReplaceWorkspaceNode(conceptEntry.Handle, concept, concept with { Generation = replacement })])
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

    static WorkspaceSyntaxEntry? GeneratedProperty(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is PropertySyntax { IsGenerated: true }).ToArray();

        return subjects.Length == 1 ? subjects[0] : null;
    }

    static WorkspaceSyntaxEntry? LowEntropyConcept(WorkspaceSyntaxIndex index, PropertySyntax property)
    {
        var concepts = index.Entries.Where(entry => entry.Node is ConceptSyntax concept && concept.Name == property.Type.Name).ToArray();
        if (concepts.Length != 1 || concepts[0].Node is not ConceptSyntax { Generation: { } generation })
        {
            return null;
        }

        return GenerationEntropy.Bits(generation) is { } bits && bits < 64 ? concepts[0] : null;
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
