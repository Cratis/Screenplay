// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceProductionRepairs
{
    // The workspace is immutable and owns its derived compilation. Weak keys keep verification
    // snapshot-local (including attachments), without retaining old revisions or candidate workspaces.
    static readonly ConditionalWeakTable<ScreenplayWorkspace, Verification> _verification = [];

    internal static int TransactionCount(ScreenplayWorkspace workspace) => _verification.GetOrCreateValue(workspace).Transactions;

    internal static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAuthoringRequest request)
    {
        Interlocked.Increment(ref _verification.GetOrCreateValue(workspace).Transactions);
        return workspace.ProposeAuthoring(request);
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyDestination)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is ProducesSyntax && entry.Location == diagnostic.Location).ToArray();
        if (subjects.Length != 1 || index.Find(subjects[0].Parent!) is not { Node: CommandSyntax command } commandEntry ||
            index.Find(commandEntry.Parent!) is not { Node: SliceSyntax } slice)
        {
            return [];
        }

        var subject = subjects[0];
        var produces = (ProducesSyntax)subject.Node;
        if (diagnostic.Code == DiagnosticCodes.OmittedProductionDestination)
        {
            var identifiers = command.Properties.Where(property => property.IsIdentifier && !property.Type.IsOptional && !property.Type.IsCollection).ToArray();
            if (produces.When is not null || produces.For is not null || identifiers.Length != 1)
            {
                return [];
            }

            var repair = new WorkspaceDiagnosticRepair(
                diagnostic.Code,
                subject.Handle,
                [new ReplaceWorkspaceNode(subject.Handle, produces, produces with { For = new PathExpressionSyntax(identifiers[0].Name, produces.Location) })]);

            if (!verifyDestination)
            {
                return [repair];
            }

            var verified = _verification.GetOrCreateValue(index.Workspace).Subjects.GetOrAdd(
                subject.Handle,
                static (_, state) => new Lazy<bool>(() =>
                {
                    var workspace = state.Index.Workspace;
                    var proposal = Propose(workspace, new WorkspaceAuthoringRequest
                    {
                        ExpectedRevision = workspace.Revision,
                        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
                        Validation = WorkspaceAuthoringValidation.Authoring,
                        Formatting = state.Repair.RequiredFormatting,
                        Operations = state.Repair.Operations
                    });
                    return KeepsOtherDestinations(state.Index, state.Subject, proposal) && WorkspaceDroppedComments.In(proposal.WritePlan!).IsEmpty;
                }),
                (Index: index, Subject: subject, Repair: repair));

            return verified.Value ? [repair] : [];
        }

        // Compilation includes partial ASTs, but editable entries do not. Never infer a contract
        // while another document's declarations or producers might be hidden by parser errors.
        // Declared/imported contracts cannot produce PLAY0166: names resolve application-wide.
        if (index.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ||
            index.Entries.Any(entry => entry.Node is CaptureAppendSyntax append && append.Event == produces.Event))
        {
            return [];
        }

        var properties = Infer(index, command, produces);
        if (properties is null)
        {
            return [];
        }

        foreach (var other in index.Entries.Where(entry => entry.Node is ProducesSyntax production && production.Event == produces.Event))
        {
            if (other.Handle.Document != subject.Handle.Document || index.Find(other.Parent!)?.Node is not CommandSyntax producer ||
                Infer(index, producer, (ProducesSyntax)other.Node) is not { } inferred || !SameShape(properties, inferred))
            {
                return [];
            }
        }

        return [new(diagnostic.Code, subject.Handle, [new AddWorkspaceNode(slice.Handle, slice.Node, "events", new EventSyntax(produces.Event, properties, produces.Location))])];
    }

    internal static bool KeepsOtherDestinations(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry subject, WorkspaceAuthoringResult proposal)
    {
        var workspace = index.Workspace;
        if (workspace.Compilation.Value is not { } original || subject.Parent is null ||
            index.Find(subject.Parent) is not { Address: { } address } || subject.Index is null)
        {
            return false;
        }

        if (!proposal.Accepted || proposal.Workspace!.Compilation.Value is not { } candidate ||
            original.Model.LanguageVersion != candidate.Model.LanguageVersion || original.Model.SemanticVersion != candidate.Model.SemanticVersion)
        {
            return false;
        }

        var subjectCommand = original.Documents.IdentityCatalog.ResolveSemantic(address);
        var before = Commands(original.Model.Application).ToArray();
        var after = Commands(candidate.Model.Application).ToDictionary(value => value.Id);
        if (before.Length != after.Count)
        {
            return false;
        }

        foreach (var producer in before)
        {
            if (!after.TryGetValue(producer.Id, out var changed) || producer.Produces.Length != changed.Produces.Length)
            {
                return false;
            }

            for (var production = 0; production < producer.Produces.Length; production++)
            {
                if (producer.Id == subjectCommand && production == subject.Index.Value)
                {
                    continue;
                }

                // Compare effective routing, not just the nullable override: changing a command's
                // default can reroute untouched sibling productions even within the same version.
                var previous = producer.Produces[production];
                var current = changed.Produces[production];
                if (previous.EventContract != current.EventContract ||
                    (previous.Destination ?? producer.Destination?.Value) != (current.Destination ?? changed.Destination?.Value) ||
                    (previous.Destination is null ? producer.Destination?.Type : null) != (current.Destination is null ? changed.Destination?.Type : null))
                {
                    return false;
                }
            }
        }

        return true;
    }

    static IEnumerable<SemanticCommand> Commands(SemanticApplication application) => application.Modules
        .SelectMany(module => Commands(module.Features));

    static IEnumerable<SemanticCommand> Commands(IEnumerable<SemanticFeature> features) => features
        .SelectMany(feature => feature.Slices.SelectMany(slice => slice.Commands).Concat(Commands(feature.Features)));

    static PropertySyntax[]? Infer(WorkspaceSyntaxIndex index, CommandSyntax command, ProducesSyntax produces)
    {
        var properties = new List<PropertySyntax>();
        foreach (var mapping in produces.Mappings)
        {
            if (mapping.Property.Contains('.', StringComparison.Ordinal) || properties.Exists(property => property.Name == mapping.Property))
            {
                return null;
            }

            var type = mapping.Source switch
            {
                ContextExpressionSyntax { Path: "occurred" } => new TypeRefSyntax("DateTime", false, false, mapping.Location),
                PathExpressionSyntax path => Resolve(index, command.Properties, path.Path),
                _ => null
            };
            if (type is null || !KnownType(index, type.Name))
            {
                return null;
            }

            properties.Add(new(mapping.Property, type, mapping.Location));
        }

        return [.. properties];
    }

    static TypeRefSyntax? Resolve(WorkspaceSyntaxIndex index, IEnumerable<PropertySyntax> properties, string path)
    {
        var segments = path.Split('.');
        TypeRefSyntax? result = null;
        for (var segment = 0; segment < segments.Length; segment++)
        {
            var matches = properties.Where(property => property.Name == segments[segment]).ToArray();
            if (matches.Length != 1)
            {
                return null;
            }

            result = matches[0].Type;
            if (segment < segments.Length - 1)
            {
                var types = index.Entries.Select(entry => entry.Node).OfType<TypeSyntax>().Where(type => type.Name == result.Name).ToArray();
                if (result.IsCollection || result.IsOptional || types.Length != 1)
                {
                    return null;
                }

                properties = types[0].Properties;
            }
        }

        return result;
    }

    static bool KnownType(WorkspaceSyntaxIndex index, string name) => ConceptSyntax.PrimitiveTypes.Contains(name) ||
        index.Entries.Count(entry => (entry.Node is ConceptSyntax concept && concept.Name == name) || (entry.Node is TypeSyntax type && type.Name == name)) == 1;

    static bool SameShape(PropertySyntax[] left, PropertySyntax[] right) => left.Length == right.Length && left.All(property =>
        right.Any(other => other.Name == property.Name && other.Type.Name == property.Type.Name &&
            other.Type.IsCollection == property.Type.IsCollection && other.Type.IsOptional == property.Type.IsOptional));

    sealed class Verification
    {
        internal int Transactions;
        internal ConcurrentDictionary<WorkspaceNodeHandle, Lazy<bool>> Subjects { get; } = [];
    }
}
