// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Establishes explicit author-selected destinations, without guessing intent or claiming preservation.
/// </summary>
public static class WorkspaceProductionDestinationRepairs
{
    /// <summary>
    /// Lists original event occurrences requiring complete intent, not executable repair operations.
    /// </summary>
    /// <param name="workspace">The original source snapshot.</param>
    /// <returns>Discovery rows in command and authored production order.</returns>
    public static ImmutableArray<ProductionDestinationDescriptor> Discover(ScreenplayWorkspace workspace)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (!CompleteSource(index)) return [];

        return [.. index.Entries.Where(entry => entry.Node is CommandSyntax)
            .SelectMany(entry => Describe(index, entry))];
    }

    /// <summary>
    /// Validates a complete selection set in one fresh executable authoring transaction.
    /// </summary>
    /// <param name="workspace">The unchanged base snapshot.</param>
    /// <param name="subject">The containing command from discovery.</param>
    /// <param name="intent">The author's complete destination and route confirmations.</param>
    /// <param name="request">Expected revisions and explicit canonical formatting consent.</param>
    /// <returns>A complete candidate and routing report, or conflicts with no writable partial candidate.</returns>
    public static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceNodeHandle subject, ProductionDestinationIntent intent, WorkspaceAuthoringRequest request)
    {
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return workspace.ProposeAuthoring(request with { Operations = [], Documents = [] });
        }
        if (request.Validation != WorkspaceAuthoringValidation.Executable)
        {
            return Refuse("Production destination intent requires whole-workspace Executable validation.");
        }
        if (request.Formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return Refuse("Canonical formatting consent is required.", WorkspaceConflictKind.FormattingConsentRequired);
        }
        if (!request.Operations.IsEmpty || !request.Documents.IsEmpty || !request.SemanticRenames.IsEmpty || !request.EventRenames.IsEmpty ||
            !request.RetiredSemanticAddresses.IsEmpty || !request.RetiredEventAddresses.IsEmpty || !request.EventRevisionAdvancements.IsEmpty)
        {
            return Refuse("Destination repair cannot include auxiliary edits or identity migrations.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        var commandEntry = index.Find(subject);
        if (commandEntry is not { Node: CommandSyntax command, Address: { } address } ||
            !CompleteSource(index) ||
            index.Entries.Count(entry => address.Equals(entry.Address)) != 1)
        {
            return Refuse("The command handle is stale, ambiguous, or lacks complete physical source evidence.");
        }
        var descriptors = Describe(index, commandEntry);
        if (descriptors.IsEmpty) return Refuse("No PLAY0470 occurrence addresses this command.");
        if (intent.Version != 1 || intent.Productions.IsDefault || intent.Productions.Length != descriptors.Length ||
            intent.Productions.Any(selection => selection is null || selection.Subject is null || selection.For is null || selection.Route is null) ||
            intent.Productions.Select(selection => selection.Subject).Distinct().Count() != descriptors.Length)
        {
            return Refuse("Intent v1 requires exactly one non-null selection per event-production occurrence.");
        }

        var operations = ImmutableArray.CreateBuilder<WorkspaceAstOperation>();
        foreach (var descriptor in descriptors)
        {
            var selection = intent.Productions.SingleOrDefault(selection => selection.Subject == descriptor.Subject);
            if (selection is null || descriptor.Route is null || selection.Route != descriptor.Route)
                return Refuse("Missing, foreign or stale occurrence, or mismatched/ambiguous named route confirmation.");
            var produced = (ProducesSyntax)index.Find(descriptor.Subject)!.Node;
            if (produced.For is not null && !SyntaxJson.StructurallyEqual(produced.For, selection.For))
                return Refuse("An explicit destination cannot be retargeted by this repair.");
            if (produced.For is null) operations.Add(new ReplaceWorkspaceNode(descriptor.Subject, produced, produced with { For = selection.For }));
        }

        // Do not use the intent-free discovery verification cache.
        var result = WorkspaceRepairVerification.RequireComments(workspace.ProposeAuthoring(request with { Operations = operations.ToImmutable() }));
        if (!result.Accepted) return result;
        var candidate = result.Workspace!;
        if (!ExistingIdentitiesSurvive(workspace.IdentityCatalog, candidate.IdentityCatalog))
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.InvalidIdentityMigration, "Existing identity assignments must survive unchanged.");
        var compilation = candidate.Compilation.Value!;
        var commandId = candidate.IdentityCatalog.ResolveSemantic(address);
        var bound = Commands(compilation.Model.Application).SingleOrDefault(value => value.Id == commandId);
        if (bound is null || bound.Produces.Length != descriptors.Length ||
            candidate.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ExplicitProducesTargetsRequired))
        {
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.CompilationFailed, "The candidate does not bind the complete selected event-production set.");
        }

        var rows = ImmutableArray.CreateBuilder<ProductionDestinationRow>();
        for (var ordinal = 0; ordinal < descriptors.Length; ordinal++)
        {
            var descriptor = descriptors[ordinal];
            var selection = intent.Productions.Single(selection => selection.Subject == descriptor.Subject);
            var production = bound.Produces[ordinal];
            var property = selection.For is PathExpressionSyntax path ? bound.Properties.SingleOrDefault(property => property.Name == path.Path) : null;
            var route = production.Route ?? bound.Route;
            if (property?.Type.IsOptional != false || property.Type.IsCollection ||
                (production.Destination ?? bound.Destination?.Value) != SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id) ||
                !MatchesRoute(index, candidate, descriptor.Route!, route))
            {
                return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.CompilationFailed, "A bound destination or route differs from the submitted intent.");
            }
            var original = (ProducesSyntax)index.Find(descriptor.Subject)!.Node;
            string? omission = null;
            if (original.For is null)
            {
                omission = original.InlineEvent is not null ? "inline identifier default" : "plain legacy omission; allocation/inheritance depends on binding";
            }
            rows.Add(new(
                descriptor,
                selection,
                omission,
                original.For is null ? "MadeExplicit" : "ExplicitUnchanged",
                "EstablishedFromInvalidModel",
                property,
                production with { Route = route }));
        }

        return result with { ProductionDestinationReport = new(intent, compilation.Model.LanguageVersion, compilation.Model.SemanticVersion, bound, rows.ToImmutable(), NewIdentities(workspace.IdentityCatalog, candidate.IdentityCatalog)) };
    }

    static ImmutableArray<ProductionDestinationDescriptor> Describe(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry entry)
    {
        if (entry.Node is not CommandSyntax command || entry.Parent is null || index.Find(entry.Parent)?.Node is not SliceSyntax slice) return [];
        var productions = index.Entries.Where(production => production.Parent == entry.Handle && production.Node is ProducesSyntax produced && index.Productions.IsEventProduction(produced, slice)).ToArray();
        var diagnostics = index.RepairableDiagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ExplicitProducesTargetsRequired && productions.Any(production => production.Location == diagnostic.Location)).ToImmutableArray();
        if (diagnostics.IsEmpty) return [];

        return [.. productions.Select(production =>
        {
            var produced = (ProducesSyntax)production.Node;
            var effective = produced.Stream ?? command.Stream;
            return new ProductionDestinationDescriptor(entry.Handle, diagnostics, production.Handle, produced.Event, produced.When, produced.For, produced.Stream, effective, ResolveRoute(index, effective));
        })];
    }

    static ProductionDestinationRoute? ResolveRoute(WorkspaceSyntaxIndex index, CommandStreamSyntax? route)
    {
        if (route is null) return new("Legacy");
        var sources = index.Entries.Where(entry => entry.Node is EventSourceSyntax source && source.Name == route.EventSource).ToArray();
        if (sources is not [var sourceEntry]) return null;
        var streams = index.Entries.Where(entry => entry.Parent == sourceEntry.Handle && entry.Node is EventStreamSyntax stream && stream.Name == route.Stream).ToArray();

        return streams is [var streamEntry] ? new("Named", sourceEntry.Handle, streamEntry.Handle) : null;
    }

    static bool MatchesRoute(WorkspaceSyntaxIndex index, ScreenplayWorkspace candidate, ProductionDestinationRoute confirmation, SemanticCommandRoute? route) =>
        confirmation.Kind == "Legacy" ? route is null : route is not null &&
        index.Find(confirmation.Source!)?.Address is { } source && index.Find(confirmation.Stream!)?.Address is { } stream &&
        route.Source == candidate.IdentityCatalog.ResolveSemantic(source) && route.Stream == candidate.IdentityCatalog.ResolveSemantic(stream);

    static bool CompleteSource(WorkspaceSyntaxIndex index) => index.UnresolvedPlacementDocuments.IsEmpty &&
        index.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error || diagnostic.Code == DiagnosticCodes.ExplicitProducesTargetsRequired);

    static bool ExistingIdentitiesSurvive(SemanticIdentityCatalog before, SemanticIdentityCatalog after) =>
        before.Application == after.Application && before.Documents.All(after.Documents.Contains) &&
        before.Semantics.All(after.Semantics.Contains) && before.EventContracts.All(after.EventContracts.Contains);

    static ImmutableArray<string> NewIdentities(SemanticIdentityCatalog before, SemanticIdentityCatalog after) =>
        [.. after.Documents.Except(before.Documents).Select(value => value.Id.ToString()),
         .. after.Semantics.Except(before.Semantics).Select(value => value.Id.ToString()),
         .. after.EventContracts.Except(before.EventContracts).Select(value => value.Id.ToString())];

    static IEnumerable<SemanticCommand> Commands(SemanticApplication application) => application.Modules.SelectMany(module => Commands(module.Features));
    static IEnumerable<SemanticCommand> Commands(IEnumerable<SemanticFeature> features) => features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Commands).Concat(Commands(feature.Features)));
    static WorkspaceAuthoringResult Refuse(string message, WorkspaceConflictKind kind = WorkspaceConflictKind.InvalidOperation) => new() { Conflicts = [new() { Kind = kind, Message = message }] };
}
