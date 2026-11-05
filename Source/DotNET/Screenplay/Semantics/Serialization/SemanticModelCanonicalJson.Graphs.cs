// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the ESM v38 members: graphs, graph projections and graph specifications. Each is written only when the model holds it, so the bytes
/// of models before v38 do not change.
/// </summary>
public static partial class SemanticModelCanonicalJson
{
    static void WriteGraphs(Utf8JsonWriter writer, SemanticApplication application, SemanticVersion version)
    {
        if (SemanticModelValidator.UsesGraphs(application) && version.Major < 38)
        {
            throw new InvalidSemanticContract("Graphs, graph projections and graph specifications require ESM v38 or later.");
        }

        if (!application.Graphs.IsDefaultOrEmpty) WriteArray(writer, "graphs", application.Graphs.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraph);
        if (!application.GraphProjections.IsDefaultOrEmpty) WriteArray(writer, "graphProjections", application.GraphProjections.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraphProjection);
        if (!application.GraphSpecifications.IsDefaultOrEmpty) WriteArray(writer, "graphSpecifications", application.GraphSpecifications.OrderBy(_ => _.Owner, StringComparer.Ordinal).ThenBy(_ => _.Name, StringComparer.Ordinal), WriteGraphSpecification);
    }

    static void WriteGraph(Utf8JsonWriter writer, SemanticGraph graph)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", graph.Name);
        WriteArray(writer, "nodes", graph.Nodes.OrderBy(_ => _.Label, StringComparer.Ordinal), (output, node) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "label", node.Label);
            WriteArray(output, "properties", node.Properties.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraphProperty);
            output.WriteEndObject();
        });
        WriteArray(writer, "edges", graph.Edges.OrderBy(_ => _.Type, StringComparer.Ordinal), (output, edge) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "type", edge.Type);
            CanonicalJson.WriteString(output, "sourceLabel", edge.Source);
            CanonicalJson.WriteString(output, "targetLabel", edge.Target);
            if (edge.IdentifiedBy is not null) CanonicalJson.WriteString(output, "identityProperty", edge.IdentifiedBy);
            WriteArray(output, "properties", edge.Properties.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraphProperty);
            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static void WriteGraphProperty(Utf8JsonWriter writer, SemanticGraphProperty property)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", property.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, property.Type);
        writer.WriteBoolean("key", property.IsKey);
        writer.WriteEndObject();
    }

    static void WriteGraphProjection(Utf8JsonWriter writer, SemanticGraphProjection projection)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", projection.Name);
        CanonicalJson.WriteString(writer, "graph", projection.Graph);
        WriteArray(writer, "nodes", projection.Nodes.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraphElementHandlers);
        WriteArray(writer, "edges", projection.Edges.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteGraphElementHandlers);
        writer.WriteEndObject();
    }

    static void WriteGraphElementHandlers(Utf8JsonWriter writer, SemanticGraphElementHandlers element)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", element.Name);
        WriteArray(writer, "handlers", element.Handlers, WriteGraphHandler);
        writer.WriteEndObject();
    }

    static void WriteGraphHandler(Utf8JsonWriter writer, SemanticGraphHandler handler)
    {
        writer.WriteStartObject();
        writer.WriteString("event", handler.Event.ToString());
        WriteOptionalProjectionValue(writer, "key", handler.Key);
        WriteOptionalProjectionValue(writer, "source", handler.Source);
        WriteOptionalProjectionValue(writer, "target", handler.Target);
        WriteOptionalProjectionValue(writer, "identity", handler.Identity);
        if (handler.Removes) writer.WriteBoolean("removes", true);
        if (handler.Detach) writer.WriteBoolean("detach", true);
        WriteArray(writer, "mappings", handler.Mappings, (output, mapping) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "name", mapping.Property);
            output.WriteString("operation", ProjectionOperation(mapping.Operation));
            WriteOptionalProjectionValue(output, "source", mapping.Source);
            WriteOptionalProjectionValue(output, "factor", mapping.Factor);
            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static void WriteOptionalProjectionValue(Utf8JsonWriter writer, string name, SemanticProjectionValue? value)
    {
        if (value is null) return;
        writer.WritePropertyName(name);
        WriteProjectionValue(writer, value);
    }

    static void WriteGraphSpecification(Utf8JsonWriter writer, SemanticGraphSpecification specification)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "owner", specification.Owner);
        CanonicalJson.WriteString(writer, "name", specification.Name);
        WriteArray(writer, "given", specification.Given, WriteSpecificationEvent);
        WriteArray(writer, "givenGraphs", specification.GivenGraphs, WriteGraphStep);
        if (specification.WhenAppended is not null)
        {
            writer.WritePropertyName("whenAppended");
            WriteSpecificationEvent(writer, specification.WhenAppended);
        }

        WriteArray(writer, "thenGraphs", specification.ThenGraphs, WriteGraphStep);
        writer.WriteEndObject();
    }

    static void WriteGraphStep(Utf8JsonWriter writer, SemanticGraphStep step)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "graph", step.Graph);
        WriteArray(writer, "elements", step.Elements, (output, element) =>
        {
            output.WriteStartObject();
            output.WriteBoolean("node", element.IsNode);
            output.WriteBoolean("absent", element.Absent);
            CanonicalJson.WriteString(output, "name", element.Name);
            output.WritePropertyName("key");
            WriteValue(output, element.Key);
            if (element.Target is not null)
            {
                output.WritePropertyName("target");
                WriteValue(output, element.Target);
            }

            if (element.Identity is not null)
            {
                output.WritePropertyName("identity");
                WriteValue(output, element.Identity);
            }

            WriteArray(output, "values", element.Values, (item, value) =>
            {
                item.WriteStartObject();
                CanonicalJson.WriteString(item, "name", value.Property);
                item.WritePropertyName("value");
                WriteValue(item, value.Value);
                item.WriteEndObject();
            });
            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }
}
