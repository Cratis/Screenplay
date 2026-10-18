// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpLineageTrace
{
    static readonly string[] _producerKinds = ["Command", "Reaction", "Capture"];

    static string Incomplete(WorkspacePropertyProjectionEffect effect)
    {
        if (effect.Mapping.Property.Contains('.')) return $"'{effect.Mapping.Property}' is a nested path, which the index does not resolve.";
        if (effect.Mapping is SetMappingSyntax { Source: RawExpressionSyntax }) return "The mapping is a raw expression, which the index does not resolve.";
        if (effect.Sources.FirstOrDefault(source => source.Members.Length == 0) is { } unresolved) return $"'{unresolved.Path}' does not resolve to a member of the feeding event.";
        if (effect.Events.Length == 0) return "The feeding event does not resolve to a declared event.";

        return "The mapping sits in a child or nested projection scope, which the trace does not resolve.";
    }

    static McpLineageStep Ambiguous(McpLineageStep step, string name, McpDeclaration[] candidates) =>
        step with { Status = "ambiguous", Candidates = candidates, Reason = $"'{name}' resolves to {candidates.Length} declarations; none is followed." };

    IEnumerable<Referrer> Referencing(McpDeclaration declaration, params string[] roles) => _index.Incoming(declaration)
        .Where(reference => roles.Contains(reference.Reference.Role, StringComparer.Ordinal) && reference.Reference.Owner is not null && reference.Candidates.Length == 1)
        .Select(reference => new Referrer(reference.Reference, reference.Reference.Owner!))
        .DistinctBy(referrer => (referrer.Owner.Kind, referrer.Owner.Address));

    IEnumerable<McpLineageStep> Readers(McpDeclaration property)
    {
        foreach (var query in Referencing(OwnerOf(property)!, "queryResult"))
        {
            yield return new("query", "resolved", query.Owner.Kind, query.Owner.Address, query.Reference.Location) { Text = $"query {query.Owner.Name}" };
        }
        foreach (var link in _links.Incoming(property).Where(link => link.Reference.Role == "screen-binding" && link.Reference.Owner is not null))
        {
            var owner = link.Reference.Owner!;
            yield return new("screen-display", "resolved", owner.Kind, owner.Address, link.Reference.Location) { Text = McpLineageText.Clause(_links.NodeAt(link.Occurrence), link.Reference.Name) };
        }
    }

    IEnumerable<McpLineageStep> ReadModelSources(McpDeclaration property, McpDeclaration model)
    {
        var builders = Referencing(model, "builds", "buildsVariant").Select(builder => (builder, Declaration: Declaration(builder.Owner))).ToArray();
        var steps = builders.Where(item => item.Declaration?.Syntax is ReducerSyntax)
            .Select(item => McpLineageStep.Break("opaque", "code", item.builder.Owner.Kind, item.builder.Owner.Address, item.Declaration!.Location, $"Reducer {item.builder.Owner.Name} builds {model.Name} in code; the trace does not read what the code assigns to {property.Name}."))
            .Concat(_links.ProjectionEffects.Where(effect => effect.Targets.Any(target => ReferenceEquals(target, property))).Select(ProjectionMapping))
            .ToArray();
        if (steps.Length > 0) return steps;
        var projections = builders.Where(item => item.Declaration?.Syntax is ProjectionSyntax).ToArray();
        if (projections.Length > 0)
        {
            return projections.Select(item => McpLineageStep.Break("missing", "projection-mapping", item.builder.Owner.Kind, item.builder.Owner.Address, item.builder.Reference.Location, $"Projection {item.builder.Owner.Name} builds {model.Name}, but no mapping or AutoMap sets {property.Name}."));
        }
        var performers = Referencing(model, "queryResult").Select(query => (query, Declaration: Declaration(query.Owner))).Where(item => item.Declaration?.Syntax is QuerySyntax { Performer: not null }).ToArray();
        if (performers.Length > 0)
        {
            return performers.Select(item => McpLineageStep.Break("opaque", "code", item.query.Owner.Kind, item.query.Owner.Address, item.Declaration!.Location, $"Query {item.query.Owner.Name} returns {model.Name} from its performer code; the trace does not read what the code sets {property.Name} to."));
        }

        return [McpLineageStep.Break("missing", "projection-mapping", model.Kind, model.Address, property.Location, $"Nothing builds {model.Name}, so no projection mapping sets {property.Name}.")];
    }

    McpLineageStep ProjectionMapping(WorkspacePropertyProjectionEffect effect)
    {
        var projection = _links.OwnerOf(effect.Feed);
        var location = effect.AutoMapped ? effect.Feed.Node.Location : effect.Mapping.Location;
        var step = new McpLineageStep("projection-mapping", "resolved", projection?.Kind, projection?.Address, location)
        {
            Mode = effect.AutoMapped ? "automap" : "explicit",
            Block = effect.Feed.Node switch { JoinEventSyntax => "join", AllSyntax => "all", _ => "from" },
            Text = effect.AutoMapped ? $"automap {string.Join('|', effect.Events.Select(@event => @event.Name))}.{effect.Sources.FirstOrDefault()?.Path}" : McpLineageText.Mapping(effect.Mapping)
        };
        if (effect.Events.Length > 1) return Ambiguous(step, string.Join('|', effect.Events.Select(@event => @event.Name)), effect.Events);
        if (effect.Targets.Length > 1) return Ambiguous(step, effect.Mapping.Property, effect.Targets);
        if (effect.Sources.FirstOrDefault(source => source.Members.Length > 1) is { } ambiguous) return Ambiguous(step, ambiguous.Path, ambiguous.Members);
        if (!effect.Complete) return step with { Status = "incomplete", Reason = Incomplete(effect) };
        var members = effect.Sources.Select(source => source.Members[0]).Distinct().Select(Member).ToArray();
        if (members.Length > 0) return step with { Next = () => members };
        var terminal = McpLineageText.Terminal(effect.Mapping, location);

        return terminal is null
            ? step with { Status = "incomplete", Reason = "The mapping value is neither an event member nor a literal, context or operation value the trace recognizes." }
            : step with { Next = () => [terminal] };
    }

    sealed record Referrer(McpReference Reference, McpReadOwner Owner);
}
