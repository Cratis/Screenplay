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
        if (Target(index, revision, diagnostic) is not { } target)
        {
            return [];
        }

        return [.. RaiseRepairs(index, diagnostic, target, verify), .. UniqueRepairs(index, diagnostic, target, verify)];
    }

    static GenerationTarget? Target(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        if (GeneratedProperty(index, revision, diagnostic) is not { } subject) return null;
        if (index.Find(subject.Parent!) is not { Node: CommandSyntax command } owner) return null;
        if (index.Find(owner.Parent!) is not { Node: SliceSyntax } sliceEntry) return null;

        return LowEntropyConcept(index, (PropertySyntax)subject.Node) is { } conceptEntry ? new(subject, command, sliceEntry, conceptEntry) : null;
    }

    static ImmutableArray<WorkspaceDiagnosticRepair> RaiseRepairs(WorkspaceSyntaxIndex index, Diagnostic diagnostic, GenerationTarget target, bool verify)
    {
        var concept = (ConceptSyntax)target.Concept.Node;
        if (Raise(concept.Generation!) is not { } replacement)
        {
            return [];
        }

        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, target.Concept.Handle, [new ReplaceWorkspaceNode(target.Concept.Handle, concept, concept with { Generation = replacement })])
        {
            Title = "Raise generation to at least 64 random bits",
            CanFixAll = false
        };

        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }

    static ImmutableArray<WorkspaceDiagnosticRepair> UniqueRepairs(WorkspaceSyntaxIndex index, Diagnostic diagnostic, GenerationTarget target, bool verify)
    {
        var slice = (SliceSyntax)target.Slice.Node;
        if (diagnostic.Code != DiagnosticCodes.LowGenerationEntropy || Unique(index, target.Command, (PropertySyntax)target.Property.Node, slice) is not { } constraint)
        {
            return [];
        }

        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, target.Property.Handle, [new AddWorkspaceNode(target.Slice.Handle, slice, "constraints", constraint)])
        {
            Title = "Add a unique constraint for the generated property",
            CanFixAll = false
        };

        return WorkspaceRepairVerification.Discover(index, repair, verify);
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

    sealed record GenerationTarget(WorkspaceSyntaxEntry Property, CommandSyntax Command, WorkspaceSyntaxEntry Slice, WorkspaceSyntaxEntry Concept);
}
