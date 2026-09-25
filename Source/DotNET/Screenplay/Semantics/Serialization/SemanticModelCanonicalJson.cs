// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    public const string Schema = "cratis.screenplay.esm";
    internal const uint SchemaVersion = 1;

    internal static byte[] Serialize(ExecutableSemanticModel model)
    {
        SemanticModelValidator.Validate(model.Application, model.SemanticVersion);
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

#if DEBUG
    internal static byte[] SerializeExpressionVector(ImmutableArray<SemanticExpression> expressions)
    {
        try
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, CanonicalJson.WriterOptions);
            writer.WriteStartArray();
            foreach (var expression in expressions)
            {
                WriteExpression(writer, expression);
            }

            writer.WriteEndArray();
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidSemanticContract($"The semantic expression vector exceeds the canonical maximum depth of {CanonicalJson.MaximumDepth}.");
        }
    }
#endif

    static byte[] Write(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticRevision? revision,
        SemanticApplication application)
    {
        try
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, CanonicalJson.WriterOptions);
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("schemaVersion", languageVersion.Major);
            writer.WriteString("languageVersion", languageVersion.ToString());
            writer.WriteString("semanticVersion", semanticVersion.ToString());
            if (revision is not null)
            {
                writer.WriteString("revision", revision.Value.ToString());
            }

            writer.WritePropertyName("application");
            WriteApplication(writer, application, semanticVersion);
            writer.WriteEndObject();
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidSemanticContract($"The semantic model could not be serialized: {exception.Message}");
        }
    }

    static void WriteApplication(Utf8JsonWriter writer, SemanticApplication application, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, application.Id);
        CanonicalJson.WriteString(writer, "name", application.Name);
        WriteArray(writer, "concepts", application.Concepts.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteConcept);
        WriteArray(writer, "types", application.Types.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteCompositeType);
        WriteArray(writer, "modules", application.Modules.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, module) => WriteModule(output, module, version));
        if (!application.Policies.IsDefaultOrEmpty) WriteArray(writer, "policies", application.Policies.OrderBy(_ => _.Name, StringComparer.Ordinal), WritePolicy);
        writer.WriteEndObject();
    }

    static void WriteConcept(Utf8JsonWriter writer, SemanticConcept concept)
    {
        writer.WriteStartObject();
        WriteId(writer, concept.Id);
        CanonicalJson.WriteString(writer, "name", concept.Name);
        writer.WriteString("primitive", Primitive(concept.Primitive));
        WriteStringArray(writer, "values", concept.Values);
        WriteArray(writer, "validations", concept.Validations, WriteValidation);
        writer.WriteEndObject();
    }

    static void WriteCompositeType(Utf8JsonWriter writer, SemanticCompositeType type)
    {
        writer.WriteStartObject();
        WriteId(writer, type.Id);
        CanonicalJson.WriteString(writer, "name", type.Name);
        WriteArray(writer, "properties", type.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteModule(Utf8JsonWriter writer, SemanticModule module, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, module.Id);
        CanonicalJson.WriteString(writer, "name", module.Name);
        WriteArray(writer, "features", module.Features.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, feature) => WriteFeature(output, feature, version));
        writer.WriteEndObject();
    }

    static void WriteFeature(Utf8JsonWriter writer, SemanticFeature feature, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, feature.Id);
        CanonicalJson.WriteString(writer, "name", feature.Name);
        WriteArray(writer, "features", feature.Features.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, nested) => WriteFeature(output, nested, version));
        WriteArray(writer, "slices", feature.Slices.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, slice) => WriteSlice(output, slice, version));
        writer.WriteEndObject();
    }

    static void WriteSlice(Utf8JsonWriter writer, SemanticSlice slice, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, slice.Id);
        CanonicalJson.WriteString(writer, "name", slice.Name);
        writer.WriteString("kind", SliceKind(slice.Kind));
        WriteArray(writer, "events", slice.Events.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, @event) => WriteEvent(output, @event, version));
        WriteArray(writer, "commands", slice.Commands.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteCommand);
        WriteArray(writer, "readModels", slice.ReadModels.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteReadModel);
        WriteArray(writer, "projections", slice.Projections.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, projection) => WriteProjection(output, projection, version));
        if (!slice.Reducers.IsEmpty) WriteArray(writer, "reducers", slice.Reducers.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteReducer);
        WriteArray(writer, "queries", slice.Queries.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteQuery);
        WriteArray(writer, "specifications", slice.Specifications.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteSpecification);
        WriteConstraints(writer, slice.Constraints);
        writer.WriteEndObject();
    }

    static void WriteReducer(Utf8JsonWriter writer, SemanticReducer reducer)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", reducer.Name);
        writer.WriteString("readModel", reducer.ReadModel.ToString());
        writer.WriteString("key", "eventSourceId");
        writer.WriteNull("initialState");
        writer.WriteString("result", "stateOrDelete");
        WriteArray(writer, "transitions", reducer.Transitions.OrderBy(_ => _.EventContract.ToString(), StringComparer.Ordinal), (output, transition) =>
        {
            output.WriteStartObject();
            output.WriteString("eventContract", transition.EventContract.ToString());
            CanonicalJson.WriteString(output, "requirementId", transition.RequirementId);
            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static void WriteProperty(Utf8JsonWriter writer, SemanticProperty property)
    {
        writer.WriteStartObject();
        WriteId(writer, property.Id);
        CanonicalJson.WriteString(writer, "name", property.Name);
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
        WriteSeverity(writer, validation.Severity);
        if (validation.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)
        {
            CanonicalJson.WriteString(writer, "name", validation.Name!);
            CanonicalJson.WriteString(writer, "requirementId", validation.RequirementId!);
        }
        writer.WriteEndObject();
    }

    static void WriteEvent(Utf8JsonWriter writer, SemanticEventContract eventContract, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, eventContract.Id);
        writer.WriteString("contractId", eventContract.ContractId.ToString());
        writer.WriteNumber("contractRevision", eventContract.Revision.Value);
        CanonicalJson.WriteString(writer, "name", eventContract.Name);
        WriteArray(writer, "properties", eventContract.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        if (!eventContract.Tags.IsDefaultOrEmpty) WriteStringArray(writer, "tags", eventContract.Tags);
        if (version == SemanticVersion.V4 && !eventContract.PriorRevisions.IsDefaultOrEmpty)
        {
            writer.WriteNumber("predecessor", eventContract.Predecessor!.Value.Value);
            WriteArray(writer, "priorRevisions", eventContract.PriorRevisions, (output, prior) =>
            {
                output.WriteStartObject();
                output.WriteNumber("contractRevision", prior.Revision.Value);
                if (prior.Predecessor is { } predecessor) output.WriteNumber("predecessor", predecessor.Value);
                else output.WriteNull("predecessor");
                WriteArray(output, "properties", prior.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
                if (!prior.Tags.IsDefaultOrEmpty) WriteStringArray(output, "tags", prior.Tags);
                output.WriteEndObject();
            });
        }
        writer.WriteEndObject();
    }

    static void WriteCommand(Utf8JsonWriter writer, SemanticCommand command)
    {
        writer.WriteStartObject();
        WriteId(writer, command.Id);
        CanonicalJson.WriteString(writer, "name", command.Name);
        WriteArray(writer, "properties", command.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        WriteArray(writer, "validations", command.Validations, WriteValidation);
        if (!command.CodeValidations.IsEmpty)
        {
            WriteArray(writer, "codeValidations", command.CodeValidations, (output, block) =>
            {
                output.WriteStartObject();
                CanonicalJson.WriteString(output, "requirementId", block.RequirementId);
                output.WriteEndObject();
            });
        }
        WriteArray(writer, "produces", command.Produces, WriteProducedEvent);
        if (!command.Requirements.IsDefaultOrEmpty) WriteArray(writer, "requirements", command.Requirements, WriteRequirement);
        if (command.Authorization is not null)
        {
            writer.WritePropertyName("authorization");
            WriteAuthorization(writer, command.Authorization);
        }
        if (command.Destination is not null)
        {
            writer.WritePropertyName("destination");
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            WriteTypeReference(writer, command.Destination.Type);
            WriteOptionalExpression(writer, "value", command.Destination.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    static void WriteProducedEvent(Utf8JsonWriter writer, SemanticProducedEvent produced)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", produced.EventContract.ToString());
        WriteOptionalExpression(writer, "condition", produced.Condition);
        WriteOptionalExpression(writer, "destination", produced.Destination);
        WriteArray(writer, "mappings", produced.Mappings, WriteMapping);
        if (produced.When is not null)
        {
            writer.WritePropertyName("when");
            WriteCondition(writer, produced.When);
        }

        if (!produced.Tags.IsDefaultOrEmpty) WriteStringArray(writer, "tags", produced.Tags);
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
        CanonicalJson.WriteString(writer, "name", readModel.Name);
        WriteArray(writer, "properties", readModel.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteProjection(Utf8JsonWriter writer, SemanticProjection projection, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, projection.Id);
        CanonicalJson.WriteString(writer, "name", projection.Name);
        writer.WriteString("readModel", projection.ReadModel.ToString());
        WriteArray(writer, "transitions", projection.Transitions, (output, transition) => WriteTransition(output, transition, version));
        WriteProjectionScope(writer, projection.Scope);
        writer.WriteEndObject();
    }

    static void WriteTransition(Utf8JsonWriter writer, SemanticProjectionTransition transition, SemanticVersion version)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", transition.EventContract.ToString());
        writer.WritePropertyName("affectedInstance");
        writer.WriteStartObject();
        if (version != SemanticVersion.V4) writer.WriteString("cardinality", AffectedCardinality(transition.AffectedInstance.Cardinality));
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
        CanonicalJson.WriteString(writer, "name", query.Name);
        writer.WriteString("readModel", query.ReadModel.ToString());
        writer.WritePropertyName("argument");
        writer.WriteStartObject();
        WriteId(writer, query.Argument.Id);
        CanonicalJson.WriteString(writer, "name", query.Argument.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, query.Argument.Type);
        writer.WriteEndObject();
        writer.WriteString("keyProperty", query.KeyProperty.ToString());
        writer.WriteString("cardinality", QueryCardinality(query.Cardinality));
        writer.WriteString("delivery", QueryDelivery(query.Delivery));
        if (query.Authorization is not null)
        {
            writer.WritePropertyName("authorization");
            WriteAuthorization(writer, query.Authorization);
        }
        writer.WriteEndObject();
    }

    static void WriteSpecification(Utf8JsonWriter writer, SemanticSpecification specification)
    {
        writer.WriteStartObject();
        WriteId(writer, specification.Id);
        CanonicalJson.WriteString(writer, "name", specification.Name);
        WriteArray(writer, "givenEvents", specification.GivenEvents, WriteSpecificationEvent);
        WriteArray(writer, "givenReadModels", specification.GivenReadModels, WriteSpecificationReadModel);
        if (specification.GivenCaller is not null)
        {
            writer.WritePropertyName("givenCaller");
            WriteCaller(writer, specification.GivenCaller);
        }
        if (specification.When is not null)
        {
            writer.WritePropertyName("when");
            WriteSpecificationCommand(writer, specification.When);
        }
        if (specification.WhenAppended is not null)
        {
            writer.WritePropertyName("whenAppended");
            WriteSpecificationAppend(writer, specification.WhenAppended);
        }
        WriteArray(writer, "thenEvents", specification.ThenEvents, WriteSpecificationEvent);
        if (specification.ThenEventsInAnyOrder) writer.WriteBoolean("thenEventsInAnyOrder", true);
        WriteArray(writer, "thenReadModels", specification.ThenReadModels, WriteSpecificationReadModel);
        WriteArray(writer, "thenQueries", specification.ThenQueries, WriteSpecificationQuery);
        WriteArray(writer, "thenErrors", specification.ThenErrors, WriteSpecificationError);
        if (specification.ThenDenied) writer.WriteBoolean("thenDenied", true);
        writer.WriteEndObject();
    }

    static void WriteSpecificationAppend(Utf8JsonWriter writer, SemanticSpecificationAppend value)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", value.EventContract.ToString());
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        if (value.EventSource is not null) WriteEventSource(writer, value.EventSource);
        writer.WriteEndObject();
    }

    static void WriteSpecificationEvent(Utf8JsonWriter writer, SemanticSpecificationEvent value)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", value.EventContract.ToString());
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        if (value.EventSource is not null) WriteEventSource(writer, value.EventSource);
        writer.WriteEndObject();
    }

    static void WriteEventSource(Utf8JsonWriter writer, SemanticEventSourceIdentity source)
    {
        writer.WritePropertyName("eventSource");
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        WriteTypeReference(writer, source.Type);
        writer.WritePropertyName("value");
        WriteValue(writer, source.Value);
        writer.WriteEndObject();
    }

    static void WriteSpecificationCommand(Utf8JsonWriter writer, SemanticSpecificationCommand value)
    {
        writer.WriteStartObject();
        writer.WriteString("command", value.Command.ToString());
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        if (value.EventSource is not null) WriteEventSource(writer, value.EventSource);
        writer.WriteEndObject();
    }

    static void WriteSpecificationReadModel(Utf8JsonWriter writer, SemanticSpecificationReadModel value)
    {
        writer.WriteStartObject();
        writer.WriteString("readModel", value.ReadModel.ToString());
        writer.WritePropertyName("key");
        WriteValue(writer, value.Key);
        WriteArray(writer, "values", value.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
        if (value.Exactly) writer.WriteBoolean("exactly", true);
        writer.WriteEndObject();
    }

    static void WriteSpecificationQuery(Utf8JsonWriter writer, SemanticSpecificationQueryResult value)
    {
        writer.WriteStartObject();
        writer.WriteString("query", value.Query.ToString());
        writer.WritePropertyName("key");
        WriteValue(writer, value.Key);
        WriteArray(writer, "results", value.Results, WriteSpecificationReadModel);
        if (value.Exactly) writer.WriteBoolean("exactly", true);
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
            case SemanticEventContextExpression context:
                writer.WriteString("contextValue", ContextValue(context.Value));
                writer.WritePropertyName("type");
                WriteTypeReference(writer, context.Type);
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
                CanonicalJson.WriteString(writer, "value", text.Value);
                break;
            case SemanticNumberValue number:
                CanonicalJson.WriteDecimal(writer, "value", number.Value);
                break;
            case SemanticBooleanValue boolean:
                writer.WriteBoolean("value", boolean.Value);
                break;
            case SemanticArrayValue array:
                if (array.Values.IsDefault || array.Values.Any(_ => _ is null))
                {
                    throw new InvalidSemanticContract("A semantic array value is malformed.");
                }

                WriteArray(writer, "values", array.Values, WriteValue);
                break;
            case SemanticCompositeValue objectValue:
                if (objectValue.Properties.IsDefault ||
                    objectValue.Properties.Any(_ => _ is not { TargetProperty.IsSet: true, Value: not null }) ||
                    objectValue.Properties.Select(_ => _.TargetProperty).Distinct().Count() != objectValue.Properties.Length)
                {
                    throw new InvalidSemanticContract("A semantic object value is malformed.");
                }

                WriteArray(
                    writer,
                    "properties",
                    objectValue.Properties.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal),
                    WritePropertyValue);
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
            CanonicalJson.WriteString(writer, name, value);
        }
    }

    static void WriteStringArray(Utf8JsonWriter writer, string name, ImmutableArray<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            CanonicalJson.WriteStringValue(writer, value);
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

    static void WriteSeverity(Utf8JsonWriter writer, SemanticValidationSeverity severity)
    {
        switch (severity)
        {
            case SemanticValidationSeverity.Error: break;
            case SemanticValidationSeverity.Warning: writer.WriteString("severity", "warning"); break;
            case SemanticValidationSeverity.Information: writer.WriteString("severity", "information"); break;
            default: throw Unknown(nameof(SemanticValidationSeverity), severity);
        }
    }

    static string ValidationKind(SemanticValidationRuleKind value) => value switch
    {
        SemanticValidationRuleKind.NotEmpty => "notEmpty",
        SemanticValidationRuleKind.Maximum => "maximum",
        SemanticValidationRuleKind.Minimum => "minimum",
        SemanticValidationRuleKind.Equal => "equal",
        SemanticValidationRuleKind.NotEqual => "notEqual",
        SemanticValidationRuleKind.GreaterThan => "greaterThan",
        SemanticValidationRuleKind.GreaterThanOrEqual => "greaterThanOrEqual",
        SemanticValidationRuleKind.LessThan => "lessThan",
        SemanticValidationRuleKind.LessThanOrEqual => "lessThanOrEqual",
        SemanticValidationRuleKind.Length => "length",
        SemanticValidationRuleKind.AllGreaterThan => "allGreaterThan",
        SemanticValidationRuleKind.AllGreaterThanOrEqual => "allGreaterThanOrEqual",
        SemanticValidationRuleKind.Matches => "matches",
        SemanticValidationRuleKind.RulePredicate => "rulePredicate",
        SemanticValidationRuleKind.CodeValidation => "codeValidation",
        _ => throw Unknown(nameof(SemanticValidationRuleKind), value)
    };

    static string ContextValue(SemanticEventContextValueKind kind) => kind switch
    {
        SemanticEventContextValueKind.EventSourceIdentity => "eventSourceIdentity",
        SemanticEventContextValueKind.Occurred => "occurred",
        SemanticEventContextValueKind.CausedBySubject => "causedBySubject",
        SemanticEventContextValueKind.CausedByName => "causedByName",
        SemanticEventContextValueKind.CausedByUserName => "causedByUserName",
        _ => throw Unknown(nameof(SemanticEventContextValueKind), kind)
    };

    static string ExpressionKind(SemanticExpressionKind value) => value switch
    {
        SemanticExpressionKind.Value => "value",
        SemanticExpressionKind.Resolved => "resolved",
        SemanticExpressionKind.EventContext => "eventContext",
        _ => throw Unknown(nameof(SemanticExpressionKind), value)
    };

    static string ExpressionRoot(SemanticExpressionRootKind value) => value switch
    {
        SemanticExpressionRootKind.Command => "command",
        SemanticExpressionRootKind.Event => "event",
        _ => throw Unknown(nameof(SemanticExpressionRootKind), value)
    };

    static string ExpressionSource(SemanticExpressionSourceKind value) => value switch
    {
        SemanticExpressionSourceKind.Property => "property",
        _ => throw Unknown(nameof(SemanticExpressionSourceKind), value)
    };

    static string ValueKind(SemanticValueKind value) => value switch
    {
        SemanticValueKind.Null => "null",
        SemanticValueKind.Text => "string",
        SemanticValueKind.Number => "number",
        SemanticValueKind.Boolean => "boolean",
        SemanticValueKind.Array => "array",
        SemanticValueKind.Composite => "object",
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
