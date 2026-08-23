// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

static class SemanticModelCanonicalJson
{
    internal const string Schema = "cratis.screenplay.esm";
    internal const uint SchemaVersion = 1;

    internal static byte[] Serialize(ExecutableSemanticModel model)
    {
        SemanticModelValidator.Validate(model.Application);
        var expected = SemanticRevision.Compute(SerializeWithoutRevision(model.LanguageVersion, model.SemanticVersion, model.Application));
        if (model.Revision != expected)
        {
            throw new InvalidSemanticContract($"Semantic revision '{model.Revision}' does not match computed revision '{expected}'.");
        }

        return Write(model.LanguageVersion, model.SemanticVersion, model.Revision, model.Application);
    }

    internal static byte[] SerializeWithoutRevision(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticApplication application) =>
        Write(languageVersion, semanticVersion, null, application);

    static byte[] Write(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticRevision? revision,
        SemanticApplication application)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new() { Indented = false, SkipValidation = false });
        writer.WriteStartObject();
        writer.WriteString("schema", Schema);
        writer.WriteNumber("schemaVersion", SchemaVersion);
        writer.WriteString("languageVersion", languageVersion.ToString());
        writer.WriteString("semanticVersion", semanticVersion.ToString());
        if (revision is not null)
        {
            writer.WriteString("revision", revision.Value.ToString());
        }

        writer.WritePropertyName("application");
        WriteApplication(writer, application);
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    static void WriteApplication(Utf8JsonWriter writer, SemanticApplication application)
    {
        writer.WriteStartObject();
        WriteId(writer, application.Id);
        writer.WriteString("name", application.Name);
        WriteArray(writer, "concepts", application.Concepts.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteConcept);
        WriteArray(writer, "types", application.Types.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteCompositeType);
        WriteArray(writer, "modules", application.Modules.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteModule);
        writer.WriteEndObject();
    }

    static void WriteConcept(Utf8JsonWriter writer, SemanticConcept concept)
    {
        writer.WriteStartObject();
        WriteId(writer, concept.Id);
        writer.WriteString("name", concept.Name);
        writer.WriteString("primitive", Primitive(concept.Primitive));
        WriteStringArray(writer, "values", concept.Values);
        WriteArray(writer, "validations", concept.Validations, WriteValidation);
        writer.WriteEndObject();
    }

    static void WriteCompositeType(Utf8JsonWriter writer, SemanticCompositeType type)
    {
        writer.WriteStartObject();
        WriteId(writer, type.Id);
        writer.WriteString("name", type.Name);
        WriteArray(writer, "properties", type.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteModule(Utf8JsonWriter writer, SemanticModule module)
    {
        writer.WriteStartObject();
        WriteId(writer, module.Id);
        writer.WriteString("name", module.Name);
        WriteArray(writer, "features", module.Features.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteFeature);
        writer.WriteEndObject();
    }

    static void WriteFeature(Utf8JsonWriter writer, SemanticFeature feature)
    {
        writer.WriteStartObject();
        WriteId(writer, feature.Id);
        writer.WriteString("name", feature.Name);
        WriteArray(writer, "features", feature.Features.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteFeature);
        WriteArray(writer, "slices", feature.Slices.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteSlice);
        writer.WriteEndObject();
    }

    static void WriteSlice(Utf8JsonWriter writer, SemanticSlice slice)
    {
        writer.WriteStartObject();
        WriteId(writer, slice.Id);
        writer.WriteString("name", slice.Name);
        writer.WriteString("kind", SliceKind(slice.Kind));
        WriteArray(writer, "events", slice.Events.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteEvent);
        WriteArray(writer, "commands", slice.Commands.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteCommand);
        WriteArray(writer, "readModels", slice.ReadModels.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteReadModel);
        WriteArray(writer, "projections", slice.Projections.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProjection);
        WriteArray(writer, "queries", slice.Queries.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteQuery);
        WriteArray(writer, "specifications", slice.Specifications.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteSpecification);
        writer.WriteEndObject();
    }

    static void WriteProperty(Utf8JsonWriter writer, SemanticProperty property)
    {
        writer.WriteStartObject();
        WriteId(writer, property.Id);
        writer.WriteString("name", property.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, property.Type);
        writer.WriteBoolean("identifier", property.IsIdentifier);
        writer.WriteEndObject();
    }

    static void WriteTypeReference(Utf8JsonWriter writer, SemanticTypeReference type)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", TypeReferenceKind(type.Kind));
        if (type.Kind == SemanticTypeReferenceKind.Primitive)
        {
            writer.WriteString("primitive", Primitive(type.Primitive));
            writer.WriteNull("target");
        }
        else
        {
            writer.WriteNull("primitive");
            writer.WriteString("target", type.Target.ToString());
        }

        writer.WriteBoolean("collection", type.IsCollection);
        writer.WriteBoolean("optional", type.IsOptional);
        writer.WriteEndObject();
    }

    static void WriteValidation(Utf8JsonWriter writer, SemanticValidationRule validation)
    {
        writer.WriteStartObject();
        WriteOptionalSemanticId(writer, "property", validation.Property);
        writer.WriteString("kind", ValidationKind(validation.Kind));
        WriteOptionalValue(writer, "operand", validation.Operand);
        WriteOptionalString(writer, "message", validation.Message);
        writer.WriteEndObject();
    }

    static void WriteEvent(Utf8JsonWriter writer, SemanticEventContract eventContract)
    {
        writer.WriteStartObject();
        WriteId(writer, eventContract.Id);
        writer.WriteString("contractId", eventContract.ContractId.ToString());
        writer.WriteNumber("contractRevision", eventContract.Revision.Value);
        writer.WriteString("name", eventContract.Name);
        WriteArray(writer, "properties", eventContract.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteCommand(Utf8JsonWriter writer, SemanticCommand command)
    {
        writer.WriteStartObject();
        WriteId(writer, command.Id);
        writer.WriteString("name", command.Name);
        WriteArray(writer, "properties", command.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        WriteArray(writer, "validations", command.Validations, WriteValidation);
        WriteArray(writer, "produces", command.Produces, WriteProducedEvent);
        writer.WriteEndObject();
    }

    static void WriteProducedEvent(Utf8JsonWriter writer, SemanticProducedEvent produced)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", produced.EventContract.ToString());
        WriteOptionalExpression(writer, "condition", produced.Condition);
        WriteOptionalExpression(writer, "destination", produced.Destination);
        WriteArray(writer, "mappings", produced.Mappings, WriteMapping);
        writer.WriteEndObject();
    }

    static void WriteMapping(Utf8JsonWriter writer, SemanticPropertyMapping mapping)
    {
        writer.WriteStartObject();
        writer.WriteString("targetProperty", mapping.TargetProperty.ToString());
        writer.WritePropertyName("source");
        WriteExpression(writer, mapping.Source);
        writer.WriteEndObject();
    }

    static void WriteReadModel(Utf8JsonWriter writer, SemanticReadModel readModel)
    {
        writer.WriteStartObject();
        WriteId(writer, readModel.Id);
        writer.WriteString("name", readModel.Name);
        WriteArray(writer, "properties", readModel.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteProjection(Utf8JsonWriter writer, SemanticProjection projection)
    {
        writer.WriteStartObject();
        WriteId(writer, projection.Id);
        writer.WriteString("name", projection.Name);
        writer.WriteString("readModel", projection.ReadModel.ToString());
        WriteArray(writer, "transitions", projection.Transitions, WriteTransition);
        writer.WriteEndObject();
    }

    static void WriteTransition(Utf8JsonWriter writer, SemanticProjectionTransition transition)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", transition.EventContract.ToString());
        writer.WritePropertyName("affectedInstance");
        writer.WriteStartObject();
        writer.WriteString("cardinality", AffectedCardinality(transition.AffectedInstance.Cardinality));
        writer.WritePropertyName("key");
        WriteExpression(writer, transition.AffectedInstance.Key);
        writer.WriteEndObject();
        WriteArray(writer, "mappings", transition.Mappings, WriteMapping);
        writer.WriteEndObject();
    }

    static void WriteQuery(Utf8JsonWriter writer, SemanticKeyedQuery query)
    {
        writer.WriteStartObject();
        WriteId(writer, query.Id);
        writer.WriteString("name", query.Name);
        writer.WriteString("readModel", query.ReadModel.ToString());
        writer.WritePropertyName("argument");
        writer.WriteStartObject();
        WriteId(writer, query.Argument.Id);
        writer.WriteString("name", query.Argument.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, query.Argument.Type);
        writer.WriteEndObject();
        writer.WriteString("keyProperty", query.KeyProperty.ToString());
        writer.WriteString("cardinality", QueryCardinality(query.Cardinality));
        writer.WriteString("delivery", QueryDelivery(query.Delivery));
        writer.WriteEndObject();
    }

    static void WriteSpecification(Utf8JsonWriter writer, SemanticSpecification specification)
    {
        writer.WriteStartObject();
        WriteId(writer, specification.Id);
        writer.WriteString("name", specification.Name);
        WriteArray(writer, "givenEvents", specification.GivenEvents, WriteSpecificationEvent);
        WriteArray(writer, "givenReadModels", specification.GivenReadModels, WriteSpecificationReadModel);
        writer.WritePropertyName("when");
        WriteSpecificationCommand(writer, specification.When);
        WriteArray(writer, "thenEvents", specification.ThenEvents, WriteSpecificationEvent);
        WriteArray(writer, "thenReadModels", specification.ThenReadModels, WriteSpecificationReadModel);
        WriteArray(writer, "thenQueries", specification.ThenQueries, WriteSpecificationQuery);
        WriteArray(writer, "thenErrors", specification.ThenErrors, WriteSpecificationError);
        writer.WriteEndObject();
    }

    static void WriteSpecificationEvent(Utf8JsonWriter writer, SemanticSpecificationEvent value)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", value.EventContract.ToString());
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        writer.WriteEndObject();
    }

    static void WriteSpecificationCommand(Utf8JsonWriter writer, SemanticSpecificationCommand value)
    {
        writer.WriteStartObject();
        writer.WriteString("command", value.Command.ToString());
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        writer.WriteEndObject();
    }

    static void WriteSpecificationReadModel(Utf8JsonWriter writer, SemanticSpecificationReadModel value)
    {
        writer.WriteStartObject();
        writer.WriteString("readModel", value.ReadModel.ToString());
        writer.WritePropertyName("key");
        WriteValue(writer, value.Key);
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        writer.WriteEndObject();
    }

    static void WriteSpecificationQuery(Utf8JsonWriter writer, SemanticSpecificationQueryResult value)
    {
        writer.WriteStartObject();
        writer.WriteString("query", value.Query.ToString());
        writer.WritePropertyName("key");
        WriteValue(writer, value.Key);
        WriteArray(writer, "results", value.Results, WriteSpecificationReadModel);
        writer.WriteEndObject();
    }

    static void WriteSpecificationError(Utf8JsonWriter writer, SemanticSpecificationError value)
    {
        writer.WriteStartObject();
        WriteOptionalString(writer, "code", value.Code);
        WriteOptionalString(writer, "message", value.Message);
        writer.WriteEndObject();
    }

    static void WritePropertyValue(Utf8JsonWriter writer, SemanticPropertyValue value)
    {
        writer.WriteStartObject();
        writer.WriteString("targetProperty", value.TargetProperty.ToString());
        writer.WritePropertyName("value");
        WriteValue(writer, value.Value);
        writer.WriteEndObject();
    }

    static void WriteExpression(Utf8JsonWriter writer, SemanticExpression expression)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", ExpressionKind(expression.Kind));
        switch (expression)
        {
            case SemanticValueExpression value:
                writer.WritePropertyName("value");
                WriteValue(writer, value.Value);
                break;
            case SemanticResolvedExpression resolved:
                writer.WriteString("root", ExpressionRoot(resolved.Root));
                writer.WriteString("source", ExpressionSource(resolved.Source));
                writer.WriteString("target", resolved.Target.ToString());
                break;
            default:
                throw new InvalidSemanticContract("A semantic expression variant is malformed or unknown.");
        }

        writer.WriteEndObject();
    }

    static void WriteValue(Utf8JsonWriter writer, SemanticValue value)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", ValueKind(value.Kind));
        switch (value)
        {
            case SemanticNullValue:
                break;
            case SemanticTextValue text:
                writer.WriteString("value", text.Value);
                break;
            case SemanticNumberValue number:
                writer.WriteNumber("value", number.Value);
                break;
            case SemanticBooleanValue boolean:
                writer.WriteBoolean("value", boolean.Value);
                break;
            default:
                throw new InvalidSemanticContract("A semantic value variant is malformed or unknown.");
        }

        writer.WriteEndObject();
    }

    static void WriteOptionalExpression(Utf8JsonWriter writer, string name, SemanticExpression? expression)
    {
        writer.WritePropertyName(name);
        if (expression is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteExpression(writer, expression);
        }
    }

    static void WriteOptionalValue(Utf8JsonWriter writer, string name, SemanticValue? value)
    {
        writer.WritePropertyName(name);
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteValue(writer, value);
        }
    }

    static void WriteId(Utf8JsonWriter writer, SemanticId id) => writer.WriteString("id", id.ToString());

    static void WriteOptionalSemanticId(Utf8JsonWriter writer, string name, SemanticId id)
    {
        if (id.IsSet)
        {
            writer.WriteString(name, id.ToString());
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    static void WriteStringArray(Utf8JsonWriter writer, string name, ImmutableArray<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    static void WriteArray<T>(Utf8JsonWriter writer, string name, IEnumerable<T> values, Action<Utf8JsonWriter, T> write)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            write(writer, value);
        }

        writer.WriteEndArray();
    }

    static string SliceKind(SemanticSliceKind value) => value switch
    {
        SemanticSliceKind.StateChange => "stateChange",
        SemanticSliceKind.StateView => "stateView",
        _ => throw Unknown(nameof(SemanticSliceKind), value)
    };

    static string Primitive(SemanticPrimitiveType value) => value switch
    {
        SemanticPrimitiveType.Uuid => "uuid",
        SemanticPrimitiveType.Text => "string",
        SemanticPrimitiveType.WholeNumber => "integer",
        SemanticPrimitiveType.DecimalNumber => "decimal",
        SemanticPrimitiveType.Boolean => "boolean",
        SemanticPrimitiveType.Date => "date",
        SemanticPrimitiveType.DateTime => "dateTime",
        _ => throw Unknown(nameof(SemanticPrimitiveType), value)
    };

    static string TypeReferenceKind(SemanticTypeReferenceKind value) => value switch
    {
        SemanticTypeReferenceKind.Primitive => "primitive",
        SemanticTypeReferenceKind.Concept => "concept",
        SemanticTypeReferenceKind.CompositeType => "compositeType",
        _ => throw Unknown(nameof(SemanticTypeReferenceKind), value)
    };

    static string ValidationKind(SemanticValidationRuleKind value) => value switch
    {
        SemanticValidationRuleKind.NotEmpty => "notEmpty",
        SemanticValidationRuleKind.Maximum => "maximum",
        SemanticValidationRuleKind.Minimum => "minimum",
        SemanticValidationRuleKind.Equal => "equal",
        SemanticValidationRuleKind.NotEqual => "notEqual",
        SemanticValidationRuleKind.Matches => "matches",
        _ => throw Unknown(nameof(SemanticValidationRuleKind), value)
    };

    static string ExpressionKind(SemanticExpressionKind value) => value switch
    {
        SemanticExpressionKind.Value => "value",
        SemanticExpressionKind.Resolved => "resolved",
        _ => throw Unknown(nameof(SemanticExpressionKind), value)
    };

    static string ExpressionRoot(SemanticExpressionRootKind value) => value switch
    {
        SemanticExpressionRootKind.Command => "command",
        SemanticExpressionRootKind.Event => "event",
        SemanticExpressionRootKind.Query => "query",
        _ => throw Unknown(nameof(SemanticExpressionRootKind), value)
    };

    static string ExpressionSource(SemanticExpressionSourceKind value) => value switch
    {
        SemanticExpressionSourceKind.Property => "property",
        SemanticExpressionSourceKind.Argument => "argument",
        _ => throw Unknown(nameof(SemanticExpressionSourceKind), value)
    };

    static string ValueKind(SemanticValueKind value) => value switch
    {
        SemanticValueKind.Null => "null",
        SemanticValueKind.Text => "string",
        SemanticValueKind.Number => "number",
        SemanticValueKind.Boolean => "boolean",
        _ => throw Unknown(nameof(SemanticValueKind), value)
    };

    static string AffectedCardinality(AffectedInstanceCardinality value) => value switch
    {
        AffectedInstanceCardinality.One => "one",
        AffectedInstanceCardinality.ZeroOrOne => "zeroOrOne",
        AffectedInstanceCardinality.Many => "many",
        _ => throw Unknown(nameof(AffectedInstanceCardinality), value)
    };

    static string QueryCardinality(SemanticQueryCardinality value) => value switch
    {
        SemanticQueryCardinality.One => "one",
        SemanticQueryCardinality.ZeroOrOne => "zeroOrOne",
        SemanticQueryCardinality.Many => "many",
        _ => throw Unknown(nameof(SemanticQueryCardinality), value)
    };

    static string QueryDelivery(SemanticQueryDelivery value) => value switch
    {
        SemanticQueryDelivery.Snapshot => "snapshot",
        SemanticQueryDelivery.Live => "live",
        _ => throw Unknown(nameof(SemanticQueryDelivery), value)
    };

    static InvalidSemanticContract Unknown<T>(string name, T value) =>
        new($"Unknown {name} value '{value}'.");
}
