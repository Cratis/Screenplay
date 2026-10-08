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
        readonly HashSet<SemanticAddress> _persistedEventAddresses = [.. documents.IdentityCatalog.EventContracts.Select(value => value.Address)];

        SemanticModule BindModule(ModuleSyntax module)
        {
            if (module.Documentation is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Module '{module.Name}' documentation is authoring metadata.", module.Location);
            }

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
            if (feature.Documentation is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Feature '{feature.Name}' documentation is authoring metadata.", feature.Location);
            }

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
            if (slice.Documentation is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' documentation is authoring metadata.", slice.Location);
            }

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
                SliceType.Automation => SemanticSliceKind.Automation,
                SliceType.Translate => SemanticSliceKind.Translate,
                _ => SemanticSliceKind.Unknown
            };
            if (kind == SemanticSliceKind.Unknown)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Slice '{slice.Name}' of type '{slice.Type}' is not admitted by ESM v1.", slice.Location);
            }

            var address = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
            var id = ResolveSlice(address, slice.Location, slice.DescriptionLocation, slice.DescriptionRawLength);
            if (kind is SemanticSliceKind.Automation or SemanticSliceKind.Translate) UsesV6 = true;
            if (slice.Reactions.Any() || slice.Captures.Any()) _automationSlices[id] = (address, slice);
            var events = EventDeclarations.In(slice).Select(value => _eventDeclarations[value]).Distinct().ToArray();
            var commands = slice.Commands.Select(value =>
            {
                var bound = BindCommand(address, value, _events);
                bound = bound with { Authorization = EffectiveAuthorization(value.Authorize, bound.Properties, module, featurePath, value.Name) };
                _exampleCommands[value] = bound;

                return bound;
            }).ToImmutableArray();
            var readModels = (slice.ReadModels ?? []).Select(value => _readModelDeclarations[value].Model).ToImmutableArray();
            var projections = slice.Projections.SelectMany(value => BindProjections(address, value)).ToImmutableArray();
            var reducers = BindReducers(address, slice);
            var queries = slice.Queries.Select(value => _queryDeclarations.GetValueOrDefault(value) is { } query
                ? query with
                {
                    Authorization = EffectiveAuthorization(
                    value.Authorize, [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)], module, featurePath)
                }
                : null).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            foreach (var query in queries) _queries[query.Name] = query;
            var commandsByName = commands.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var specifications = slice.Specifications
                .Select(value => BindSpecification(address, value, commandsByName))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            ReportUnsupportedSliceMembers(address, slice);
            return new(id, slice.Name, kind, [.. events.Select(_ => _.Contract)], commands, readModels, projections, queries, specifications)
            {
                Constraints = BindConstraints(address, slice),
                Reducers = reducers
            };
        }

        BoundEvent BindEvent(SemanticAddress slice, EventSyntax[] declarations)
        {
            var current = declarations[^1];
            var address = SemanticAddress.ForEventContract(slice, current.Name);
            var semanticAssignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            var contractAssignment = documents.IdentityCatalog.ResolveEventContract(address);
            var revision = new EventContractRevision(current.Generation);
            if (_persistedEventAddresses.Contains(address) && contractAssignment.Revision != revision)
            {
                Error(
                    DiagnosticCodes.UnsupportedEventGenerationSemantics,
                    revision.Value < contractAssignment.Revision.Value
                        ? $"Event '{current.Name}' declares revision {revision.Value}, fewer than persisted catalog revision {contractAssignment.Revision.Value}."
                        : $"Event '{current.Name}' revision {revision.Value} requires an explicit catalog advancement from revision {contractAssignment.Revision.Value}.",
                    current.Location);
            }

            if (declarations.Length > 1) UsesV4 = true;
            var revisions = new List<(EventSyntax Syntax, ImmutableArray<SemanticProperty> Properties, ImmutableArray<string> Tags)>();
            foreach (var declaration in declarations)
            {
                if (declaration.Description is not null || declaration.Documentation is not null || declaration.Id is not null)
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event '{declaration.Name}' description, documentation and id are authoring metadata.", declaration.Location);
                }

                if (declaration.File is not null)
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event '{declaration.Name}' file reference is realization provenance.", declaration.File.Location);
                }

                var properties = declaration.Properties.Select(property => declarations.Length > 1
                    ? BindEventProperty(address, new EventContractRevision(declaration.Generation), property)
                    : BindProperty(address, property, false)).ToImmutableArray();
                revisions.Add((declaration, properties, BindTags(declaration.Tags)));
            }

            var latest = revisions[^1];
            Map(semanticAssignment.Id, contractAssignment.Origin, current.Location);
            return new(
                current,
                new(semanticAssignment.Id, contractAssignment.Id, revision, current.Name, latest.Properties)
                {
                    Tags = latest.Tags,
                    Predecessor = revision.Value > 1 ? new EventContractRevision(revision.Value - 1) : null,
                    PriorRevisions = [.. revisions.Take(revisions.Count - 1).Select(value => new SemanticEventRevision(
                        new EventContractRevision(value.Syntax.Generation),
                        value.Syntax.Generation > 1 ? new EventContractRevision(value.Syntax.Generation - 1) : null,
                        value.Properties) { Tags = value.Tags })]
                },
                latest.Properties.ToDictionary(_ => _.Name, StringComparer.Ordinal));
        }
    }
}
