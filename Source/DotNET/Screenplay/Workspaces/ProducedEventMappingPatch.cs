// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class ProducedEventMappingPatch
{
    internal static WorkspaceConflict? Apply(
        UpdateProducedEventMappingSource operation,
        ScreenplayWorkspace workspace,
        Dictionary<DocumentId, WorkspaceDocument> candidates)
    {
        if (!workspace.Compilation.Success || workspace.Compilation.Value is null)
        {
            return Conflict(WorkspaceConflictKind.CompilationFailed, "Mapping patches require a successfully compiled workspace.");
        }

        var ids = new[] { operation.Command, operation.ProducedEvent, operation.TargetProperty, operation.ExpectedSourceCommandProperty, operation.NewSourceCommandProperty };
        if (ids.Any(id => !workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Id == id)))
        {
            return Conflict(WorkspaceConflictKind.SemanticIdNotFound, "A mapping patch identity does not exist in the semantic catalog.");
        }

        var slices = Slices(workspace.Compilation.Value.Model.Application.Modules.SelectMany(module => module.Features)).ToArray();
        var command = slices.SelectMany(slice => slice.Commands).SingleOrDefault(value => value.Id == operation.Command);
        var producedEvent = slices.SelectMany(slice => slice.Events).SingleOrDefault(value => value.Id == operation.ProducedEvent);
        var oldSource = command?.Properties.SingleOrDefault(value => value.Id == operation.ExpectedSourceCommandProperty);
        var newSource = command?.Properties.SingleOrDefault(value => value.Id == operation.NewSourceCommandProperty);
        var target = producedEvent?.Properties.SingleOrDefault(value => value.Id == operation.TargetProperty);
        if (command is null || producedEvent is null || oldSource is null || newSource is null || target is null)
        {
            return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, "Mapping properties must belong to the addressed command and event declaration.");
        }

        var productions = command.Produces.Where(value => value.EventContract == producedEvent.Id).ToArray();
        if (productions.Length != 1 || productions[0].Condition is not null)
        {
            return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, "Exactly one unconditional production of the addressed event is required.");
        }

        var mappings = productions[0].Mappings.Where(value => value.TargetProperty == target.Id).ToArray();
        if (mappings.Length != 1 || mappings[0].Source is not SemanticResolvedExpression
            { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } source)
        {
            return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, "Only an existing direct command-property mapping can be patched.");
        }

        if (source.Target != oldSource.Id)
        {
            return Conflict(WorkspaceConflictKind.SemanticFieldValueDrift, "The mapping does not use the expected current command property.");
        }

        if (!Compatible(oldSource.Type, target.Type) || !Compatible(newSource.Type, target.Type))
        {
            return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, "Both command sources must be type-compatible with the event target.");
        }

        // Validate an independent graph edit before touching source, using the normal semantic admission rules.
        _ = Expected(workspace.Compilation.Value.Model, operation);
        return ProducedEventMappingSourcePatch.Apply(operation, workspace, candidates, command, producedEvent, target, oldSource, newSource);
    }

    internal static WorkspaceConflict? Verify(
        UpdateProducedEventMappingSource operation,
        ScreenplayWorkspace before,
        ScreenplayWorkspace candidate)
    {
        try
        {
            var expected = Expected(before.Compilation.Value!.Model, operation);
            if (candidate.IdentityCatalog.Revision != before.IdentityCatalog.Revision ||
                !Equivalent(expected, candidate.Compilation.Value!.Model))
            {
                return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, "The candidate changes semantics or identities beyond the requested mapping.");
            }

            return ProducedEventMappingSourcePatch.VerifyCanonical(candidate);
        }
        catch (InvalidSemanticContract exception)
        {
            return Conflict(WorkspaceConflictKind.UnsupportedSemanticField, exception.Message);
        }
    }

    internal static bool Equivalent(ExecutableSemanticModel expected, ExecutableSemanticModel actual) =>
        SemanticModelSerializer.Serialize(expected).AsSpan().SequenceEqual(SemanticModelSerializer.Serialize(actual));

    internal static WorkspaceConflict Conflict(WorkspaceConflictKind kind, string message) => new() { Kind = kind, Message = message };

    static bool Compatible(SemanticTypeReference source, SemanticTypeReference target) =>
        source.Kind == target.Kind && source.Primitive == target.Primitive && source.Target == target.Target &&
        source.IsCollection == target.IsCollection && (!source.IsOptional || target.IsOptional);

    static IEnumerable<SemanticSlice> Slices(IEnumerable<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));

    static ExecutableSemanticModel Expected(ExecutableSemanticModel before, UpdateProducedEventMappingSource operation) =>
        ExecutableSemanticModel.Create(before.LanguageVersion, before.SemanticVersion, before.Application with
        {
            Modules = [.. before.Application.Modules.Select(module => module with { Features = ChangeFeatures(module.Features, operation) })]
        });

    static ImmutableArray<SemanticFeature> ChangeFeatures(ImmutableArray<SemanticFeature> features, UpdateProducedEventMappingSource operation) =>
        [.. features.Select(feature => feature with
        {
            Features = ChangeFeatures(feature.Features, operation),
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(command => command.Id != operation.Command ? command : command with
                {
                    Produces = [.. command.Produces.Select(produced => produced.EventContract != operation.ProducedEvent ? produced : produced with
                    {
                        Mappings = [.. produced.Mappings.Select(mapping => mapping.TargetProperty != operation.TargetProperty ? mapping : mapping with
                        {
                            Source = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, operation.NewSourceCommandProperty)
                        })]
                    })]
                })]
            })]
        })];
}
