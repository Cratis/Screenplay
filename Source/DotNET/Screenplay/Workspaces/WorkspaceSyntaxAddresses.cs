// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceSyntaxAddresses
{
    internal static SemanticAddress? Address(SyntaxNode node, string? member, ImmutableArray<WorkspaceSyntaxEntry> ancestors, ApplicationIdentity application)
    {
        var parent = ancestors.LastOrDefault();
        var owner = parent?.Address;
        if (parent is null && node is ApplicationSyntax)
        {
            return SemanticAddress.ForApplication(application);
        }

        if (parent?.Node is ApplicationSyntax)
        {
            return node switch
            {
                ModuleSyntax module when member == "modules" => SemanticAddress.ForModule(application, module.Name),
                ConceptSyntax concept when member == "concepts" => SemanticAddress.ForConcept(application, concept.Name),
                TypeSyntax type when member == "types" => SemanticAddress.ForCompositeType(application, type.Name),
                _ => null
            };
        }

        var moduleName = ancestors.Select(entry => entry.Node).OfType<ModuleSyntax>().FirstOrDefault()?.Name;
        var features = ancestors.Select(entry => entry.Node).OfType<FeatureSyntax>().Select(ancestorFeature => ancestorFeature.Name).ToImmutableArray();
        if (node is FeatureSyntax feature && member == "features" && owner?.Kind is SemanticKind.Module or SemanticKind.Feature && moduleName is not null)
        {
            return SemanticAddress.ForFeature(application, moduleName, [.. features, feature.Name]);
        }

        if (node is SliceSyntax slice && member == "slices" && owner?.Kind == SemanticKind.Feature && moduleName is not null)
        {
            return SemanticAddress.ForSlice(application, moduleName, features, slice.Name);
        }

        if (owner?.Kind == SemanticKind.Slice)
        {
            return node switch
            {
                CommandSyntax command when member == "commands" => SemanticAddress.ForCommand(owner, command.Name),
                EventSyntax @event when member == "events" => SemanticAddress.ForEventContract(owner, @event.Name),
                ReadModelSyntax readModel when member == "readModels" => SemanticAddress.ForReadModel(owner, readModel.Name),
                ProjectionSyntax projection when member == "projections" => SemanticAddress.ForProjection(owner, projection.Name),
                QuerySyntax query when member == "queries" => SemanticAddress.ForQuery(owner, query.Name),
                SpecificationSyntax specification when member == "specifications" => SemanticAddress.ForSpecification(owner, specification.Name),
                _ => null
            };
        }

        if (node is PropertySyntax property && member == "properties" && owner?.Kind is SemanticKind.CompositeType or SemanticKind.Command or SemanticKind.EventContract or SemanticKind.ReadModel)
        {
            if (owner.Kind == SemanticKind.EventContract && parent?.Node is EventSyntax eventSyntax &&
                ancestors.Length >= 2 && ancestors[^2].Node is SliceSyntax eventSlice &&
                eventSlice.Events.Count(candidate => candidate.Name == eventSyntax.Name) > 1)
            {
                return SemanticAddress.ForEventProperty(owner, new(eventSyntax.Generation), property.Name);
            }

            return SemanticAddress.ForProperty(owner, property.Name);
        }

        if (node is QueryParameterSyntax argument && member == "by" && owner?.Kind == SemanticKind.Query)
        {
            return SemanticAddress.ForQueryArgument(owner, argument.Name);
        }

        return null;
    }

    internal static ImmutableArray<SemanticAddress> Declarations(ApplicationSyntax syntax, SemanticIdentityCatalog catalog)
    {
        var entries = WorkspaceSyntaxIndex.ForSyntax(syntax, catalog);

        // Historical event shapes share a contract address, but their generation-qualified
        // properties each claim their own address.
        var currentEvents = entries.Where(entry => entry.Node is EventSyntax && entry.Address is not null)
            .GroupBy(entry => entry.Address!)
            .Select(group => group.OrderByDescending(entry => ((EventSyntax)entry.Node).Generation).First().Handle)
            .ToHashSet();
        var groups = entries.Where(entry => entry.Address is not null &&
                (entry.Node is not EventSyntax || currentEvents.Contains(entry.Handle)))
            .GroupBy(entry => entry.Address!);
        foreach (var group in groups.Where(group => group.Count() > 1 && group.Key.Kind is not (SemanticKind.Application or SemanticKind.Module or SemanticKind.Feature)))
        {
            throw new InvalidSemanticContract($"Multiple source occurrences claim semantic {group.Key.Kind} '{group.Key.Name}'.");
        }

        return [.. groups.Select(group => group.Key).Order(WorkspaceSemanticAddressComparer.Instance)];
    }
}
