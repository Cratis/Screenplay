// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticGraph Graph(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph");
        var seen = NewSeen();
        string? name = null;
        ImmutableArray<SemanticGraphNode> nodes = default;
        ImmutableArray<SemanticGraphEdge> edges = default;
        while (NextProperty(ref reader, seen, "graph") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "nodes": nodes = Array(ref reader, GraphNode, property); break;
                case "edges": edges = Array(ref reader, GraphEdge, property); break;
                default: throw Unknown(property, "graph");
            }
        }

        Required(name is not null && !nodes.IsDefault && !edges.IsDefault, "graph");
        return new(name!, nodes, edges);
    }

    static SemanticGraphNode GraphNode(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph node");
        var seen = NewSeen();
        string? label = null;
        ImmutableArray<SemanticGraphProperty> properties = default;
        while (NextProperty(ref reader, seen, "graph node") is { } property)
        {
            switch (property)
            {
                case "label": label = String(ref reader, property); break;
                case "properties": properties = Array(ref reader, GraphProperty, property); break;
                default: throw Unknown(property, "graph node");
            }
        }

        Required(label is not null && !properties.IsDefault, "graph node");
        return new(label!, properties);
    }

    static SemanticGraphEdge GraphEdge(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph edge");
        var seen = NewSeen();
        string? type = null;
        string? source = null;
        string? target = null;
        string? identifiedBy = null;
        ImmutableArray<SemanticGraphProperty> properties = default;
        while (NextProperty(ref reader, seen, "graph edge") is { } property)
        {
            switch (property)
            {
                case "type": type = String(ref reader, property); break;
                case "sourceLabel": source = String(ref reader, property); break;
                case "targetLabel": target = String(ref reader, property); break;
                case "identityProperty": identifiedBy = String(ref reader, property); break;
                case "properties": properties = Array(ref reader, GraphProperty, property); break;
                default: throw Unknown(property, "graph edge");
            }
        }

        Required(type is not null && source is not null && target is not null && !properties.IsDefault, "graph edge");
        return new(type!, source!, target!, properties) { IdentifiedBy = identifiedBy };
    }

    static SemanticGraphProperty GraphProperty(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph property");
        var seen = NewSeen();
        string? name = null;
        SemanticTypeReference? type = null;
        bool? key = null;
        while (NextProperty(ref reader, seen, "graph property") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": type = Nested(ref reader, TypeReference, property); break;
                case "key": key = Boolean(ref reader, property); break;
                default: throw Unknown(property, "graph property");
            }
        }

        Required(name is not null && type is not null && key is not null, "graph property");
        return new(name!, type!, key!.Value);
    }

    static SemanticGraphProjection GraphProjection(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph projection");
        var seen = NewSeen();
        string? name = null;
        string? graph = null;
        ImmutableArray<SemanticGraphElementHandlers> nodes = default;
        ImmutableArray<SemanticGraphElementHandlers> edges = default;
        while (NextProperty(ref reader, seen, "graph projection") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "graph": graph = String(ref reader, property); break;
                case "nodes": nodes = Array(ref reader, GraphElementHandlers, property); break;
                case "edges": edges = Array(ref reader, GraphElementHandlers, property); break;
                default: throw Unknown(property, "graph projection");
            }
        }

        Required(name is not null && graph is not null && !nodes.IsDefault && !edges.IsDefault, "graph projection");
        return new(name!, graph!, nodes, edges);
    }

    static SemanticGraphElementHandlers GraphElementHandlers(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph element handlers");
        var seen = NewSeen();
        string? name = null;
        ImmutableArray<SemanticGraphHandler> handlers = default;
        while (NextProperty(ref reader, seen, "graph element handlers") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "handlers": handlers = Array(ref reader, GraphHandler, property); break;
                default: throw Unknown(property, "graph element handlers");
            }
        }

        Required(name is not null && !handlers.IsDefault, "graph element handlers");
        return new(name!, handlers);
    }

    static SemanticGraphHandler GraphHandler(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph handler");
        var seen = NewSeen();
        SemanticId @event = default;
        SemanticProjectionValue? key = null;
        SemanticProjectionValue? source = null;
        SemanticProjectionValue? target = null;
        SemanticProjectionValue? identity = null;
        var removes = false;
        var detach = false;
        ImmutableArray<SemanticGraphMapping> mappings = default;
        while (NextProperty(ref reader, seen, "graph handler") is { } property)
        {
            switch (property)
            {
                case "event": @event = SemanticId.Parse(String(ref reader, property)); break;
                case "key": key = Nested(ref reader, ProjectionValue, property); break;
                case "source": source = Nested(ref reader, ProjectionValue, property); break;
                case "target": target = Nested(ref reader, ProjectionValue, property); break;
                case "identity": identity = Nested(ref reader, ProjectionValue, property); break;
                case "removes": removes = OnlyTrue(ref reader, property, "graph handler"); break;
                case "detach": detach = OnlyTrue(ref reader, property, "graph handler"); break;
                case "mappings": mappings = Array(ref reader, GraphMapping, property); break;
                default: throw Unknown(property, "graph handler");
            }
        }

        Required(@event.IsSet && !mappings.IsDefault, "graph handler");
        return new(@event, mappings) { Key = key, Source = source, Target = target, Identity = identity, Removes = removes, Detach = detach };
    }

    static SemanticGraphMapping GraphMapping(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph mapping");
        var seen = NewSeen();
        string? name = null;
        SemanticProjectionOperation? operation = null;
        SemanticProjectionValue? source = null;
        SemanticProjectionValue? factor = null;
        while (NextProperty(ref reader, seen, "graph mapping") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "operation": operation = ParseProjectionOperation(String(ref reader, property)); break;
                case "source": source = Nested(ref reader, ProjectionValue, property); break;
                case "factor": factor = Nested(ref reader, ProjectionValue, property); break;
                default: throw Unknown(property, "graph mapping");
            }
        }

        Required(name is not null && operation is not null, "graph mapping");
        return new(name!, operation!.Value, source) { Factor = factor };
    }

    static SemanticGraphSpecification GraphSpecification(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph specification");
        var seen = NewSeen();
        string? owner = null;
        string? name = null;
        ImmutableArray<SemanticSpecificationEvent> given = default;
        ImmutableArray<SemanticGraphStep> givenGraphs = default;
        SemanticSpecificationEvent? whenAppended = null;
        ImmutableArray<SemanticGraphStep> thenGraphs = default;
        ImmutableArray<SemanticGraphQueryExpectation> thenQueries = [];
        string? clock = null;
        while (NextProperty(ref reader, seen, "graph specification") is { } property)
        {
            switch (property)
            {
                case "owner": owner = String(ref reader, property); break;
                case "name": name = String(ref reader, property); break;
                case "given": given = Array(ref reader, (ref Utf8JsonReader item) => SpecificationEvent(ref item, 38, false), property); break;
                case "givenGraphs": givenGraphs = Array(ref reader, GraphStep, property); break;
                case "whenAppended": RequiredToken(ref reader, JsonTokenType.StartObject, property); whenAppended = SpecificationEvent(ref reader, 38, false); break;
                case "thenGraphs": thenGraphs = Array(ref reader, GraphStep, property); break;
                case "thenQueries": thenQueries = Array(ref reader, GraphQueryExpectation, property); break;
                case "clock": clock = String(ref reader, property); break;
                default: throw Unknown(property, "graph specification");
            }
        }

        Required(owner is not null && name is not null && !given.IsDefault && !givenGraphs.IsDefault && !thenGraphs.IsDefault, "graph specification");
        return new(owner!, name!, given) { GivenGraphs = givenGraphs, WhenAppended = whenAppended, ThenGraphs = thenGraphs, ThenQueries = thenQueries, Clock = clock };
    }

    static SemanticGraphStep GraphStep(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph step");
        var seen = NewSeen();
        string? graph = null;
        ImmutableArray<SemanticGraphElementStep> elements = default;
        while (NextProperty(ref reader, seen, "graph step") is { } property)
        {
            switch (property)
            {
                case "graph": graph = String(ref reader, property); break;
                case "elements": elements = Array(ref reader, GraphElementStep, property); break;
                default: throw Unknown(property, "graph step");
            }
        }

        Required(graph is not null && !elements.IsDefault, "graph step");
        return new(graph!, elements);
    }

    static SemanticGraphElementStep GraphElementStep(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph element step");
        var seen = NewSeen();
        bool? node = null;
        bool? absent = null;
        string? name = null;
        SemanticValue? key = null;
        SemanticValue? target = null;
        SemanticValue? identity = null;
        ImmutableArray<SemanticGraphPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "graph element step") is { } property)
        {
            switch (property)
            {
                case "node": node = Boolean(ref reader, property); break;
                case "absent": absent = Boolean(ref reader, property); break;
                case "name": name = String(ref reader, property); break;
                case "key": key = Nested(ref reader, Value, property); break;
                case "target": target = Nested(ref reader, Value, property); break;
                case "identity": identity = Nested(ref reader, Value, property); break;
                case "values": values = Array(ref reader, GraphPropertyValue, property); break;
                default: throw Unknown(property, "graph element step");
            }
        }

        Required(node is not null && absent is not null && name is not null && key is not null && !values.IsDefault, "graph element step");
        return new(node!.Value, absent!.Value, name!, key!, values) { Target = target, Identity = identity };
    }

    static SemanticGraphPropertyValue GraphPropertyValue(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph property value");
        var seen = NewSeen();
        string? name = null;
        SemanticValue? value = null;
        while (NextProperty(ref reader, seen, "graph property value") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "value": value = Nested(ref reader, Value, property); break;
                default: throw Unknown(property, "graph property value");
            }
        }

        Required(name is not null && value is not null, "graph property value");
        return new(name!, value!);
    }

    static SemanticGraphQuery GraphQuery(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph query");
        var seen = NewSeen();
        string? name = null;
        string? graph = null;
        SemanticId? readModel = null;
        SemanticQueryCardinality? cardinality = null;
        SemanticQueryDelivery? delivery = null;
        ImmutableArray<SemanticGraphQueryArgument> arguments = default;
        ImmutableArray<SemanticGraphMatch> matches = default;
        ImmutableArray<SemanticGraphCondition> conditions = default;
        ImmutableArray<SemanticGraphReturn> returns = default;
        SemanticAuthorization? authorization = null;
        while (NextProperty(ref reader, seen, "graph query") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "graph": graph = String(ref reader, property); break;
                case "readModel": readModel = Identifier(ref reader, property); break;
                case "cardinality": cardinality = ParseQueryCardinality(String(ref reader, property)); break;
                case "delivery": delivery = ParseQueryDelivery(String(ref reader, property)); break;
                case "arguments": arguments = Array(ref reader, GraphQueryArgument, property); break;
                case "matches": matches = Array(ref reader, GraphMatch, property); break;
                case "conditions": conditions = Array(ref reader, GraphCondition, property); break;
                case "returns": returns = Array(ref reader, GraphReturn, property); break;
                case "authorization": RequiredToken(ref reader, JsonTokenType.StartObject, property); authorization = Authorization(ref reader); break;
                default: throw Unknown(property, "graph query");
            }
        }

        Required(name is not null && graph is not null && readModel is not null && cardinality is not null && delivery is not null && !arguments.IsDefault && !matches.IsDefault && !conditions.IsDefault && !returns.IsDefault, "graph query");
        return new(name!, graph!, readModel!.Value, cardinality!.Value, delivery!.Value, arguments, matches, conditions, returns) { Authorization = authorization };
    }

    static SemanticGraphQueryArgument GraphQueryArgument(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph query argument");
        var seen = NewSeen();
        string? name = null;
        SemanticTypeReference? type = null;
        bool? key = null;
        while (NextProperty(ref reader, seen, "graph query argument") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": type = Nested(ref reader, TypeReference, property); break;
                case "key": key = Boolean(ref reader, property); break;
                default: throw Unknown(property, "graph query argument");
            }
        }

        Required(name is not null && type is not null && key is not null, "graph query argument");
        return new(name!, type!, key!.Value);
    }

    static SemanticGraphMatch GraphMatch(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph match");
        var seen = NewSeen();
        bool? optional = null;
        SemanticGraphPatternNode? start = null;
        ImmutableArray<SemanticGraphHop> hops = default;
        while (NextProperty(ref reader, seen, "graph match") is { } property)
        {
            switch (property)
            {
                case "optional": optional = Boolean(ref reader, property); break;
                case "start": start = Nested(ref reader, GraphPatternNode, property); break;
                case "hops": hops = Array(ref reader, GraphHop, property); break;
                default: throw Unknown(property, "graph match");
            }
        }

        Required(optional is not null && start is not null && !hops.IsDefault, "graph match");
        return new(optional!.Value, start!, hops);
    }

    static SemanticGraphPatternNode GraphPatternNode(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph pattern node");
        var seen = NewSeen();
        string? alias = null;
        string? label = null;
        while (NextProperty(ref reader, seen, "graph pattern node") is { } property)
        {
            switch (property)
            {
                case "alias": alias = String(ref reader, property); break;
                case "label": label = String(ref reader, property); break;
                default: throw Unknown(property, "graph pattern node");
            }
        }

        Required(label is not null, "graph pattern node");
        return new(alias, label!);
    }

    static SemanticGraphHop GraphHop(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph hop");
        var seen = NewSeen();
        string? alias = null;
        string? type = null;
        bool? outgoing = null;
        SemanticGraphPatternNode? node = null;
        while (NextProperty(ref reader, seen, "graph hop") is { } property)
        {
            switch (property)
            {
                case "alias": alias = String(ref reader, property); break;
                case "type": type = String(ref reader, property); break;
                case "outgoing": outgoing = Boolean(ref reader, property); break;
                case "node": node = Nested(ref reader, GraphPatternNode, property); break;
                default: throw Unknown(property, "graph hop");
            }
        }

        Required(type is not null && outgoing is not null && node is not null, "graph hop");
        return new(alias, type!, outgoing!.Value, node!);
    }

    static SemanticGraphCondition GraphCondition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph condition");
        var seen = NewSeen();
        string? alias = null;
        string? name = null;
        string? argument = null;
        SemanticValue? literal = null;
        while (NextProperty(ref reader, seen, "graph condition") is { } property)
        {
            switch (property)
            {
                case "alias": alias = String(ref reader, property); break;
                case "property": name = String(ref reader, property); break;
                case "argument": argument = String(ref reader, property); break;
                case "literal": literal = Nested(ref reader, Value, property); break;
                default: throw Unknown(property, "graph condition");
            }
        }

        Required(alias is not null && name is not null, "graph condition");
        return new(alias!, name!) { Argument = argument, Literal = literal };
    }

    static SemanticGraphReturn GraphReturn(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph return");
        var seen = NewSeen();
        SemanticId? target = null;
        string? alias = null;
        string? source = null;
        while (NextProperty(ref reader, seen, "graph return") is { } property)
        {
            switch (property)
            {
                case "property": target = Identifier(ref reader, property); break;
                case "alias": alias = String(ref reader, property); break;
                case "source": source = String(ref reader, property); break;
                default: throw Unknown(property, "graph return");
            }
        }

        Required(target is not null && alias is not null && source is not null, "graph return");
        return new(target!.Value, alias!, source!);
    }

    static SemanticGraphQueryExpectation GraphQueryExpectation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph query expectation");
        var seen = NewSeen();
        string? query = null;
        bool? exactly = null;
        ImmutableArray<SemanticGraphPropertyValue> arguments = default;
        ImmutableArray<SemanticGraphRow> rows = default;
        while (NextProperty(ref reader, seen, "graph query expectation") is { } property)
        {
            switch (property)
            {
                case "query": query = String(ref reader, property); break;
                case "exactly": exactly = Boolean(ref reader, property); break;
                case "arguments": arguments = Array(ref reader, GraphPropertyValue, property); break;
                case "rows": rows = Array(ref reader, GraphRow, property); break;
                default: throw Unknown(property, "graph query expectation");
            }
        }

        Required(query is not null && exactly is not null && !arguments.IsDefault && !rows.IsDefault, "graph query expectation");
        return new(query!, exactly!.Value, arguments, rows);
    }

    static SemanticGraphRow GraphRow(ref Utf8JsonReader reader)
    {
        Object(ref reader, "graph row");
        var seen = NewSeen();
        ImmutableArray<SemanticGraphPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "graph row") is { } property)
        {
            switch (property)
            {
                case "values": values = Array(ref reader, GraphPropertyValue, property); break;
                default: throw Unknown(property, "graph row");
            }
        }

        Required(!values.IsDefault, "graph row");
        return new(values);
    }
}
