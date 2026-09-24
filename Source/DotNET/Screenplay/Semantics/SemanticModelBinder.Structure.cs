// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticModule BindModule(ModuleSyntax module)
        {
            if (module.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Module '{module.Name}' description is authoring metadata.", module.Location);
            }

            foreach (var template in module.ScreenTemplates)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Dialog template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var form in module.Forms ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Form '{form.Name}' is explicitly deferred from the backend ESM v1 profile.", form.Location);
            }

            foreach (var contribution in module.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var address = SemanticAddress.ForModule(_applicationIdentity, module.Name);
            var id = Resolve(address, module.Location);
            var features = module.Features.Select(feature => BindFeature(module.Name, [], feature)).ToImmutableArray();
            return new(id, module.Name, features);
        }

        SemanticFeature BindFeature(string module, ImmutableArray<string> parentPath, FeatureSyntax feature)
        {
            if (feature.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Feature '{feature.Name}' description is authoring metadata.", feature.Location);
            }

            foreach (var contribution in feature.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var path = parentPath.Add(feature.Name);
            var address = SemanticAddress.ForFeature(_applicationIdentity, module, path);
            var id = Resolve(address, feature.Location);
            var nested = feature.Features.Select(value => BindFeature(module, path, value)).ToImmutableArray();
            var slices = feature.Slices.Select(value => BindSlice(module, path, value)).ToImmutableArray();
            return new(id, feature.Name, nested, slices);
        }

        SemanticSlice BindSlice(string module, ImmutableArray<string> featurePath, SliceSyntax slice)
        {
            if (slice.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' description is authoring metadata.", slice.Location);
            }

            if (slice.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' file reference is realization provenance.", slice.File.Location);
            }

            var kind = slice.Type switch
            {
                SliceType.StateChange => SemanticSliceKind.StateChange,
                SliceType.StateView => SemanticSliceKind.StateView,
                _ => SemanticSliceKind.Unknown
            };
            if (kind == SemanticSliceKind.Unknown)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Slice '{slice.Name}' of type '{slice.Type}' is not admitted by ESM v1.", slice.Location);
            }

            var address = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
            var id = ResolveSlice(address, slice.Location, slice.DescriptionLocation, slice.DescriptionRawLength);
            var events = slice.Events.Select(value => _eventDeclarations[value]).ToArray();
            var commands = slice.Commands.Select(value => BindCommand(address, value, _events)).ToImmutableArray();
            var readModels = (slice.ReadModels ?? []).Select(value => _readModelDeclarations[value].Model).ToImmutableArray();
            var projections = slice.Projections.Select(value => BindProjection(address, value)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var queries = slice.Queries.Select(value => _queryDeclarations.GetValueOrDefault(value)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var commandsByName = commands.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var specifications = slice.Specifications
                .Select(value => BindSpecification(address, value, commandsByName))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            ReportUnsupportedSliceMembers(slice);
            return new(id, slice.Name, kind, [.. events.Select(_ => _.Contract)], commands, readModels, projections, queries, specifications)
            {
                Constraints = BindConstraints(slice)
            };
        }

        BoundEvent BindEvent(SemanticAddress slice, EventSyntax @event)
        {
            if (@event.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event '{@event.Name}' file reference is realization provenance.", @event.File.Location);
            }

            var tags = BindTags(@event.Tags);

            var address = SemanticAddress.ForEventContract(slice, @event.Name);
            var semanticAssignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            var contractAssignment = documents.IdentityCatalog.ResolveEventContract(address);
            Map(semanticAssignment.Id, contractAssignment.Origin, @event.Location);
            var properties = @event.Properties.Select(property => BindProperty(address, property, false)).ToImmutableArray();
            return new(
                @event,
                new(
                    semanticAssignment.Id,
                    contractAssignment.Id,
                    contractAssignment.Revision,
                    @event.Name,
                    properties) { Tags = tags },
                properties.ToDictionary(_ => _.Name, StringComparer.Ordinal));
        }
    }
}
