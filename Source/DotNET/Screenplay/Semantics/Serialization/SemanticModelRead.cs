// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

static class SemanticModelRead
{
    internal delegate T ValueReader<T>(ref Utf8JsonReader reader);

    internal static SemanticApplication Application(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticConcept> concepts = default;
        ImmutableArray<SemanticCompositeType> types = default;
        ImmutableArray<SemanticModule> modules = default;
        while (NextProperty(ref reader, seen, "application") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "concepts": concepts = Array(ref reader, Concept, property); break;
                case "types": types = Array(ref reader, CompositeType, property); break;
                case "modules": modules = Array(ref reader, Module, property); break;
                default: throw Unknown(property, "application");
            }
        }

        Required(id.IsSet && name is not null && !concepts.IsDefault && !types.IsDefault && !modules.IsDefault, "application");
        return new(id, name!, concepts, types, modules);
    }

    internal static SemanticConcept Concept(ref Utf8JsonReader reader)
    {
        Object(ref reader, "concept");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticPrimitiveType? primitive = null;
        ImmutableArray<string> values = default;
        ImmutableArray<SemanticValidationRule> validations = default;
        while (NextProperty(ref reader, seen, "concept") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "primitive": primitive = ParsePrimitive(String(ref reader, property)); break;
                case "values": values = StringArray(ref reader, property); break;
                case "validations": validations = Array(ref reader, Validation, property); break;
                default: throw Unknown(property, "concept");
            }
        }

        Required(id.IsSet && name is not null && primitive is not null && !values.IsDefault && !validations.IsDefault, "concept");
        return new(id, name!, primitive!.Value, values, validations);
    }

    internal static SemanticCompositeType CompositeType(ref Utf8JsonReader reader)
    {
        Object(ref reader, "composite type");
        var (id, name, properties) = NamedProperties(ref reader, "composite type");
        return new(id, name, properties);
    }

    internal static SemanticModule Module(ref Utf8JsonReader reader)
    {
        Object(ref reader, "module");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticFeature> features = default;
        while (NextProperty(ref reader, seen, "module") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "features": features = Array(ref reader, Feature, property); break;
                default: throw Unknown(property, "module");
            }
        }

        Required(id.IsSet && name is not null && !features.IsDefault, "module");
        return new(id, name!, features);
    }

    internal static SemanticFeature Feature(ref Utf8JsonReader reader)
    {
        Object(ref reader, "feature");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticFeature> features = default;
        ImmutableArray<SemanticSlice> slices = default;
        while (NextProperty(ref reader, seen, "feature") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "features": features = Array(ref reader, Feature, property); break;
                case "slices": slices = Array(ref reader, Slice, property); break;
                default: throw Unknown(property, "feature");
            }
        }

        Required(id.IsSet && name is not null && !features.IsDefault && !slices.IsDefault, "feature");
        return new(id, name!, features, slices);
    }

    internal static SemanticSlice Slice(ref Utf8JsonReader reader)
    {
        Object(ref reader, "slice");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticSliceKind? kind = null;
        ImmutableArray<SemanticEventContract> events = default;
        ImmutableArray<SemanticCommand> commands = default;
        ImmutableArray<SemanticReadModel> readModels = default;
        ImmutableArray<SemanticProjection> projections = default;
        ImmutableArray<SemanticKeyedQuery> queries = default;
        ImmutableArray<SemanticSpecification> specifications = default;
        while (NextProperty(ref reader, seen, "slice") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "kind": kind = ParseSliceKind(String(ref reader, property)); break;
                case "events": events = Array(ref reader, Event, property); break;
                case "commands": commands = Array(ref reader, Command, property); break;
                case "readModels": readModels = Array(ref reader, ReadModel, property); break;
                case "projections": projections = Array(ref reader, Projection, property); break;
                case "queries": queries = Array(ref reader, Query, property); break;
                case "specifications": specifications = Array(ref reader, Specification, property); break;
                default: throw Unknown(property, "slice");
            }
        }

        Required(
            id.IsSet && name is not null && kind is not null && !events.IsDefault && !commands.IsDefault &&
            !readModels.IsDefault && !projections.IsDefault && !queries.IsDefault && !specifications.IsDefault,
            "slice");
        return new(id, name!, kind!.Value, events, commands, readModels, projections, queries, specifications);
    }

    internal static SemanticProperty Property(ref Utf8JsonReader reader)
    {
        Object(ref reader, "property");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticTypeReference? type = null;
        bool? identifier = null;
        while (NextProperty(ref reader, seen, "property") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                case "identifier": identifier = Boolean(ref reader, property); break;
                default: throw Unknown(property, "property");
            }
        }

        Required(id.IsSet && name is not null && type is not null && identifier is not null, "property");
        return new(id, name!, type!, identifier!.Value);
    }

    internal static SemanticTypeReference TypeReference(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticTypeReferenceKind? kind = null;
        SemanticPrimitiveType? primitive = null;
        SemanticId target = default;
        var targetRead = false;
        bool? collection = null;
        bool? optional = null;
        while (NextProperty(ref reader, seen, "type reference") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseTypeReferenceKind(String(ref reader, property)); break;
                case "primitive": primitive = NullableString(ref reader, property) is { } primitiveText ? ParsePrimitive(primitiveText) : SemanticPrimitiveType.Unknown; break;
                case "target": targetRead = true; target = NullableString(ref reader, property) is { } targetText ? SemanticId.Parse(targetText) : default; break;
                case "collection": collection = Boolean(ref reader, property); break;
                case "optional": optional = Boolean(ref reader, property); break;
                default: throw Unknown(property, "type reference");
            }
        }

        Required(kind is not null && primitive is not null && targetRead && collection is not null && optional is not null, "type reference");
        return new(kind!.Value, primitive!.Value, target, collection!.Value, optional!.Value);
    }

    internal static SemanticValidationRule Validation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "validation");
        var seen = NewSeen();
        SemanticId propertyId = default;
        var propertyRead = false;
        SemanticValidationRuleKind? kind = null;
        SemanticValue? operand = null;
        var operandRead = false;
        string? message = null;
        var messageRead = false;
        while (NextProperty(ref reader, seen, "validation") is { } property)
        {
            switch (property)
            {
                case "property": propertyRead = true; propertyId = NullableString(ref reader, property) is { } propertyText ? SemanticId.Parse(propertyText) : default; break;
                case "kind": kind = ParseValidationKind(String(ref reader, property)); break;
                case "operand": operandRead = true; operand = NullableValue(ref reader, property); break;
                case "message": messageRead = true; message = NullableString(ref reader, property); break;
                default: throw Unknown(property, "validation");
            }
        }

        Required(propertyRead && kind is not null && operandRead && messageRead, "validation");
        return new(propertyId, kind!.Value, operand, message);
    }

    internal static SemanticEventContract Event(ref Utf8JsonReader reader)
    {
        Object(ref reader, "event contract");
        var seen = NewSeen();
        SemanticId id = default;
        EventContractId contractId = default;
        EventContractRevision revision = default;
        string? name = null;
        ImmutableArray<SemanticProperty> properties = default;
        while (NextProperty(ref reader, seen, "event contract") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "contractId": contractId = EventContractId.Parse(String(ref reader, property)); break;
                case "contractRevision": revision = new(UInt32(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "properties": properties = Array(ref reader, Property, property); break;
                default: throw Unknown(property, "event contract");
            }
        }

        Required(id.IsSet && contractId.IsSet && revision.IsValid && name is not null && !properties.IsDefault, "event contract");
        return new(id, contractId, revision, name!, properties);
    }

    internal static SemanticCommand Command(ref Utf8JsonReader reader)
    {
        Object(ref reader, "command");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticProperty> properties = default;
        ImmutableArray<SemanticValidationRule> validations = default;
        ImmutableArray<SemanticProducedEvent> produces = default;
        while (NextProperty(ref reader, seen, "command") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "properties": properties = Array(ref reader, Property, property); break;
                case "validations": validations = Array(ref reader, Validation, property); break;
                case "produces": produces = Array(ref reader, ProducedEvent, property); break;
                default: throw Unknown(property, "command");
            }
        }

        Required(id.IsSet && name is not null && !properties.IsDefault && !validations.IsDefault && !produces.IsDefault, "command");
        return new(id, name!, properties, validations, produces);
    }

    internal static SemanticProducedEvent ProducedEvent(ref Utf8JsonReader reader)
    {
        Object(ref reader, "produced event");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticExpression? condition = null;
        var conditionRead = false;
        SemanticExpression? destination = null;
        var destinationRead = false;
        ImmutableArray<SemanticPropertyMapping> mappings = default;
        while (NextProperty(ref reader, seen, "produced event") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "condition": conditionRead = true; condition = NullableExpression(ref reader, property); break;
                case "destination": destinationRead = true; destination = NullableExpression(ref reader, property); break;
                case "mappings": mappings = Array(ref reader, Mapping, property); break;
                default: throw Unknown(property, "produced event");
            }
        }

        Required(eventContract.IsSet && conditionRead && destinationRead && !mappings.IsDefault, "produced event");
        return new(eventContract, condition, destination, mappings);
    }

    internal static SemanticPropertyMapping Mapping(ref Utf8JsonReader reader)
    {
        Object(ref reader, "mapping");
        var seen = NewSeen();
        SemanticId target = default;
        SemanticExpression? source = null;
        while (NextProperty(ref reader, seen, "mapping") is { } property)
        {
            switch (property)
            {
                case "targetProperty": target = SemanticId.Parse(String(ref reader, property)); break;
                case "source": RequiredToken(ref reader, JsonTokenType.StartObject, property); source = Expression(ref reader); break;
                default: throw Unknown(property, "mapping");
            }
        }

        Required(target.IsSet && source is not null, "mapping");
        return new(target, source!);
    }

    internal static SemanticReadModel ReadModel(ref Utf8JsonReader reader)
    {
        Object(ref reader, "read model");
        var (id, name, properties) = NamedProperties(ref reader, "read model");
        return new(id, name, properties);
    }

    internal static SemanticProjection Projection(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticId readModel = default;
        ImmutableArray<SemanticProjectionTransition> transitions = default;
        while (NextProperty(ref reader, seen, "projection") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "readModel": readModel = SemanticId.Parse(String(ref reader, property)); break;
                case "transitions": transitions = Array(ref reader, Transition, property); break;
                default: throw Unknown(property, "projection");
            }
        }

        Required(id.IsSet && name is not null && readModel.IsSet && !transitions.IsDefault, "projection");
        return new(id, name!, readModel, transitions);
    }

    internal static SemanticProjectionTransition Transition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection transition");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticAffectedInstance? affected = null;
        ImmutableArray<SemanticPropertyMapping> mappings = default;
        while (NextProperty(ref reader, seen, "projection transition") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "affectedInstance": RequiredToken(ref reader, JsonTokenType.StartObject, property); affected = AffectedInstance(ref reader); break;
                case "mappings": mappings = Array(ref reader, Mapping, property); break;
                default: throw Unknown(property, "projection transition");
            }
        }

        Required(eventContract.IsSet && affected is not null && !mappings.IsDefault, "projection transition");
        return new(eventContract, affected!, mappings);
    }

    internal static SemanticAffectedInstance AffectedInstance(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        AffectedInstanceCardinality? cardinality = null;
        SemanticExpression? key = null;
        while (NextProperty(ref reader, seen, "affected instance") is { } property)
        {
            switch (property)
            {
                case "cardinality": cardinality = ParseAffectedCardinality(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = Expression(ref reader); break;
                default: throw Unknown(property, "affected instance");
            }
        }

        Required(cardinality is not null && key is not null, "affected instance");
        return new(cardinality!.Value, key!);
    }

    internal static SemanticKeyedQuery Query(ref Utf8JsonReader reader)
    {
        Object(ref reader, "query");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticId readModel = default;
        SemanticReadModelQueryArgument? argument = null;
        SemanticId keyProperty = default;
        SemanticQueryCardinality? cardinality = null;
        SemanticQueryDelivery? delivery = null;
        while (NextProperty(ref reader, seen, "query") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "readModel": readModel = SemanticId.Parse(String(ref reader, property)); break;
                case "argument": RequiredToken(ref reader, JsonTokenType.StartObject, property); argument = QueryArgument(ref reader); break;
                case "keyProperty": keyProperty = SemanticId.Parse(String(ref reader, property)); break;
                case "cardinality": cardinality = ParseQueryCardinality(String(ref reader, property)); break;
                case "delivery": delivery = ParseQueryDelivery(String(ref reader, property)); break;
                default: throw Unknown(property, "query");
            }
        }

        Required(id.IsSet && name is not null && readModel.IsSet && argument is not null && keyProperty.IsSet && cardinality is not null && delivery is not null, "query");
        return new(id, name!, argument!, readModel, keyProperty, cardinality!.Value, delivery!.Value);
    }

    internal static SemanticReadModelQueryArgument QueryArgument(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        SemanticTypeReference? type = null;
        while (NextProperty(ref reader, seen, "query argument") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                default: throw Unknown(property, "query argument");
            }
        }

        Required(id.IsSet && name is not null && type is not null, "query argument");
        return new(id, name!, type!);
    }

    internal static SemanticSpecification Specification(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticSpecificationEvent> givenEvents = default;
        ImmutableArray<SemanticSpecificationReadModel> givenReadModels = default;
        SemanticSpecificationCommand? when = null;
        ImmutableArray<SemanticSpecificationEvent> thenEvents = default;
        ImmutableArray<SemanticSpecificationReadModel> thenReadModels = default;
        ImmutableArray<SemanticSpecificationQueryResult> thenQueries = default;
        ImmutableArray<SemanticSpecificationError> thenErrors = default;
        while (NextProperty(ref reader, seen, "specification") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "givenEvents": givenEvents = Array(ref reader, SpecificationEvent, property); break;
                case "givenReadModels": givenReadModels = Array(ref reader, SpecificationReadModel, property); break;
                case "when": RequiredToken(ref reader, JsonTokenType.StartObject, property); when = SpecificationCommand(ref reader); break;
                case "thenEvents": thenEvents = Array(ref reader, SpecificationEvent, property); break;
                case "thenReadModels": thenReadModels = Array(ref reader, SpecificationReadModel, property); break;
                case "thenQueries": thenQueries = Array(ref reader, SpecificationQuery, property); break;
                case "thenErrors": thenErrors = Array(ref reader, SpecificationError, property); break;
                default: throw Unknown(property, "specification");
            }
        }

        Required(
            id.IsSet && name is not null && !givenEvents.IsDefault && !givenReadModels.IsDefault && when is not null &&
            !thenEvents.IsDefault && !thenReadModels.IsDefault && !thenQueries.IsDefault && !thenErrors.IsDefault,
            "specification");
        return new(id, name!, givenEvents, givenReadModels, when!, thenEvents, thenReadModels, thenQueries, thenErrors);
    }

    internal static SemanticSpecificationEvent SpecificationEvent(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification event");
        var seen = NewSeen();
        SemanticId eventContract = default;
        ImmutableArray<SemanticPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "specification event") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "values": values = Array(ref reader, PropertyValue, property); break;
                default: throw Unknown(property, "specification event");
            }
        }

        Required(eventContract.IsSet && !values.IsDefault, "specification event");
        return new(eventContract, values);
    }

    internal static SemanticSpecificationCommand SpecificationCommand(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticId command = default;
        ImmutableArray<SemanticPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "specification command") is { } property)
        {
            switch (property)
            {
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                case "values": values = Array(ref reader, PropertyValue, property); break;
                default: throw Unknown(property, "specification command");
            }
        }

        Required(command.IsSet && !values.IsDefault, "specification command");
        return new(command, values);
    }

    internal static SemanticSpecificationReadModel SpecificationReadModel(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification read model");
        var seen = NewSeen();
        SemanticId readModel = default;
        SemanticValue? key = null;
        ImmutableArray<SemanticPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "specification read model") is { } property)
        {
            switch (property)
            {
                case "readModel": readModel = SemanticId.Parse(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = Value(ref reader); break;
                case "values": values = Array(ref reader, PropertyValue, property); break;
                default: throw Unknown(property, "specification read model");
            }
        }

        Required(readModel.IsSet && key is not null && !values.IsDefault, "specification read model");
        return new(readModel, key!, values);
    }

    internal static SemanticSpecificationQueryResult SpecificationQuery(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification query result");
        var seen = NewSeen();
        SemanticId query = default;
        SemanticValue? key = null;
        ImmutableArray<SemanticSpecificationReadModel> results = default;
        while (NextProperty(ref reader, seen, "specification query result") is { } property)
        {
            switch (property)
            {
                case "query": query = SemanticId.Parse(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = Value(ref reader); break;
                case "results": results = Array(ref reader, SpecificationReadModel, property); break;
                default: throw Unknown(property, "specification query result");
            }
        }

        Required(query.IsSet && key is not null && !results.IsDefault, "specification query result");
        return new(query, key!, results);
    }

    internal static SemanticSpecificationError SpecificationError(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification error");
        var seen = NewSeen();
        string? code = null;
        var codeRead = false;
        string? message = null;
        var messageRead = false;
        while (NextProperty(ref reader, seen, "specification error") is { } property)
        {
            switch (property)
            {
                case "code": codeRead = true; code = NullableString(ref reader, property); break;
                case "message": messageRead = true; message = NullableString(ref reader, property); break;
                default: throw Unknown(property, "specification error");
            }
        }

        Required(codeRead && messageRead, "specification error");
        return new(code, message);
    }

    internal static SemanticPropertyValue PropertyValue(ref Utf8JsonReader reader)
    {
        Object(ref reader, "property value");
        var seen = NewSeen();
        SemanticId target = default;
        SemanticValue? value = null;
        while (NextProperty(ref reader, seen, "property value") is { } property)
        {
            switch (property)
            {
                case "targetProperty": target = SemanticId.Parse(String(ref reader, property)); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                default: throw Unknown(property, "property value");
            }
        }

        Required(target.IsSet && value is not null, "property value");
        return new(target, value!);
    }

    internal static SemanticExpression Expression(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticExpressionKind? kind = null;
        SemanticValue? value = null;
        var valueRead = false;
        SemanticExpressionRootKind? root = null;
        SemanticExpressionSourceKind? source = null;
        SemanticId target = default;
        var targetRead = false;
        while (NextProperty(ref reader, seen, "expression") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseExpressionKind(String(ref reader, property)); break;
                case "value": valueRead = true; RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                case "root": root = ParseExpressionRoot(String(ref reader, property)); break;
                case "source": source = ParseExpressionSource(String(ref reader, property)); break;
                case "target": targetRead = true; target = SemanticId.Parse(String(ref reader, property)); break;
                default: throw Unknown(property, "expression");
            }
        }

        return kind switch
        {
            SemanticExpressionKind.Value when valueRead && value is not null && root is null && source is null && !targetRead => new SemanticValueExpression(value),
            SemanticExpressionKind.Resolved when !valueRead && root is not null && source is not null && targetRead && target.IsSet => new SemanticResolvedExpression(root.Value, source.Value, target),
            _ => throw Malformed("expression", "one exact expression variant")
        };
    }

    internal static SemanticValue Value(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticValueKind? kind = null;
        SemanticValue? value = null;
        var valueRead = false;
        ImmutableArray<SemanticValue> values = default;
        var valuesRead = false;
        ImmutableArray<SemanticPropertyValue> properties = default;
        var propertiesRead = false;
        while (NextProperty(ref reader, seen, "value") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseValueKind(String(ref reader, property)); break;
                case "value":
                    valueRead = true;
                    value = kind switch
                    {
                        SemanticValueKind.Text => new SemanticTextValue(String(ref reader, property)),
                        SemanticValueKind.Number => new SemanticNumberValue(Decimal(ref reader, property)),
                        SemanticValueKind.Boolean => new SemanticBooleanValue(Boolean(ref reader, property)),
                        _ => throw Malformed("value", "a kind discriminator before its typed value")
                    };
                    break;
                case "values":
                    if (kind != SemanticValueKind.Array)
                    {
                        throw Malformed("value", "an array kind discriminator before its values");
                    }

                    valuesRead = true;
                    values = Array(ref reader, ValueElement, property);
                    break;
                case "properties":
                    if (kind != SemanticValueKind.Composite)
                    {
                        throw Malformed("value", "an object kind discriminator before its properties");
                    }

                    propertiesRead = true;
                    properties = Array(ref reader, PropertyValue, property);
                    break;
                default: throw Unknown(property, "value");
            }
        }

        return kind switch
        {
            SemanticValueKind.Null when !valueRead && !valuesRead && !propertiesRead => SemanticValue.Null,
            SemanticValueKind.Text or SemanticValueKind.Number or SemanticValueKind.Boolean
                when valueRead && value is not null && !valuesRead && !propertiesRead => value,
            SemanticValueKind.Array when !valueRead && valuesRead && !propertiesRead => new SemanticArrayValue(values),
            SemanticValueKind.Composite when !valueRead && !valuesRead && propertiesRead => new SemanticCompositeValue(properties),
            _ => throw Malformed("value", "one exact value variant")
        };
    }

    internal static SemanticValue ValueElement(ref Utf8JsonReader reader)
    {
        Object(ref reader, "semantic array element");
        return Value(ref reader);
    }

    internal static SemanticExpression? NullableExpression(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw Malformed(name, "an object or null");
        }

        return Expression(ref reader);
    }

    internal static SemanticValue? NullableValue(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw Malformed(name, "an object or null");
        }

        return Value(ref reader);
    }

    internal static (SemanticId Id, string Name, ImmutableArray<SemanticProperty> Properties) NamedProperties(
        ref Utf8JsonReader reader,
        string description)
    {
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticProperty> properties = default;
        while (NextProperty(ref reader, seen, description) is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "properties": properties = Array(ref reader, Property, property); break;
                default: throw Unknown(property, description);
            }
        }

        Required(id.IsSet && name is not null && !properties.IsDefault, description);
        return (id, name!, properties);
    }

    internal static string? NextProperty(ref Utf8JsonReader reader, HashSet<string> seen, string owner)
    {
        if (!reader.Read())
        {
            throw new InvalidSemanticContract($"The {owner} object ended unexpectedly.");
        }

        if (reader.TokenType == JsonTokenType.EndObject)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw Malformed(owner, "a property name");
        }

        var property = reader.GetString()!;
        if (!seen.Add(property))
        {
            throw new InvalidSemanticContract($"The {owner} contains duplicate property '{property}'.");
        }

        return property;
    }

    internal static void RequiredToken(ref Utf8JsonReader reader, JsonTokenType token, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != token)
        {
            throw Malformed(name, token.ToString());
        }
    }

    internal static string String(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.String)
        {
            throw Malformed(name, "a string");
        }

        return CanonicalJson.RequireNfc(reader.GetString()!, name);
    }

    internal static string? NullableString(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => CanonicalJson.RequireNfc(reader.GetString()!, name),
            _ => throw Malformed(name, "a string or null")
        };
    }

    internal static uint UInt32(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetUInt32(out var value))
        {
            throw Malformed(name, "an unsigned 32-bit integer");
        }

        return value;
    }

    internal static bool Boolean(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => throw Malformed(name, "a Boolean")
        };
    }

    internal static decimal Decimal(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var value))
        {
            throw Malformed(name, "a decimal number");
        }

        return value;
    }

    internal static decimal? NullableDecimal(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var value))
        {
            throw Malformed(name, "a decimal number or null");
        }

        return value;
    }

    internal static bool? NullableBoolean(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => throw Malformed(name, "a Boolean or null")
        };
    }

    internal static ImmutableArray<T> Array<T>(ref Utf8JsonReader reader, ValueReader<T> read, string name)
    {
        RequiredToken(ref reader, JsonTokenType.StartArray, name);
        var values = ImmutableArray.CreateBuilder<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToImmutable();
            }

            values.Add(read(ref reader));
        }

        throw new InvalidSemanticContract($"The {name} array ended unexpectedly.");
    }

    internal static ImmutableArray<string> StringArray(ref Utf8JsonReader reader, string name)
    {
        RequiredToken(ref reader, JsonTokenType.StartArray, name);
        var values = ImmutableArray.CreateBuilder<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToImmutable();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw Malformed(name, "an array of strings");
            }

            values.Add(reader.GetString()!);
        }

        throw new InvalidSemanticContract($"The {name} array ended unexpectedly.");
    }

    internal static void Object(ref Utf8JsonReader reader, string name)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw Malformed(name, "an object");
        }
    }

    internal static void RequiredRead(ref Utf8JsonReader reader, string name)
    {
        if (!reader.Read())
        {
            throw new InvalidSemanticContract($"The {name} value is missing.");
        }
    }

    internal static void Required(bool condition, string owner)
    {
        if (!condition)
        {
            throw new InvalidSemanticContract($"The {owner} is missing one or more required fields.");
        }
    }

    internal static HashSet<string> NewSeen() => new(StringComparer.Ordinal);

    internal static InvalidSemanticContract Unknown(string property, string owner) =>
        new($"Unknown property '{property}' in {owner}.");

    internal static InvalidSemanticContract Malformed(string name, string expected) =>
        new($"The {name} value must be {expected}.");

    internal static SemanticSliceKind ParseSliceKind(string value) => value switch
    {
        "stateChange" => SemanticSliceKind.StateChange,
        "stateView" => SemanticSliceKind.StateView,
        _ => throw DiscriminatorError(value, "slice kind")
    };

    internal static SemanticPrimitiveType ParsePrimitive(string value) => value switch
    {
        "uuid" => SemanticPrimitiveType.Uuid,
        "string" => SemanticPrimitiveType.Text,
        "integer" => SemanticPrimitiveType.WholeNumber,
        "decimal" => SemanticPrimitiveType.DecimalNumber,
        "boolean" => SemanticPrimitiveType.Boolean,
        "date" => SemanticPrimitiveType.Date,
        "dateTime" => SemanticPrimitiveType.DateTime,
        _ => throw DiscriminatorError(value, "primitive type")
    };

    internal static SemanticTypeReferenceKind ParseTypeReferenceKind(string value) => value switch
    {
        "primitive" => SemanticTypeReferenceKind.Primitive,
        "concept" => SemanticTypeReferenceKind.Concept,
        "compositeType" => SemanticTypeReferenceKind.CompositeType,
        _ => throw DiscriminatorError(value, "type reference kind")
    };

    internal static SemanticValidationRuleKind ParseValidationKind(string value) => value switch
    {
        "notEmpty" => SemanticValidationRuleKind.NotEmpty,
        "maximum" => SemanticValidationRuleKind.Maximum,
        "minimum" => SemanticValidationRuleKind.Minimum,
        "equal" => SemanticValidationRuleKind.Equal,
        "notEqual" => SemanticValidationRuleKind.NotEqual,
        "matches" => SemanticValidationRuleKind.Matches,
        _ => throw DiscriminatorError(value, "validation kind")
    };

    internal static SemanticExpressionKind ParseExpressionKind(string value) => value switch
    {
        "value" => SemanticExpressionKind.Value,
        "resolved" => SemanticExpressionKind.Resolved,
        _ => throw DiscriminatorError(value, "expression kind")
    };

    internal static SemanticExpressionRootKind ParseExpressionRoot(string value) => value switch
    {
        "command" => SemanticExpressionRootKind.Command,
        "event" => SemanticExpressionRootKind.Event,
        "query" => SemanticExpressionRootKind.Query,
        _ => throw DiscriminatorError(value, "expression root")
    };

    internal static SemanticExpressionSourceKind ParseExpressionSource(string value) => value switch
    {
        "property" => SemanticExpressionSourceKind.Property,
        "argument" => SemanticExpressionSourceKind.Argument,
        _ => throw DiscriminatorError(value, "expression source")
    };

    internal static SemanticValueKind ParseValueKind(string value) => value switch
    {
        "null" => SemanticValueKind.Null,
        "string" => SemanticValueKind.Text,
        "number" => SemanticValueKind.Number,
        "boolean" => SemanticValueKind.Boolean,
        "array" => SemanticValueKind.Array,
        "object" => SemanticValueKind.Composite,
        _ => throw DiscriminatorError(value, "value kind")
    };

    internal static AffectedInstanceCardinality ParseAffectedCardinality(string value) => value switch
    {
        "one" => AffectedInstanceCardinality.One,
        "zeroOrOne" => AffectedInstanceCardinality.ZeroOrOne,
        "many" => AffectedInstanceCardinality.Many,
        _ => throw DiscriminatorError(value, "affected instance cardinality")
    };

    internal static SemanticQueryCardinality ParseQueryCardinality(string value) => value switch
    {
        "one" => SemanticQueryCardinality.One,
        "zeroOrOne" => SemanticQueryCardinality.ZeroOrOne,
        "many" => SemanticQueryCardinality.Many,
        _ => throw DiscriminatorError(value, "query cardinality")
    };

    internal static SemanticQueryDelivery ParseQueryDelivery(string value) => value switch
    {
        "snapshot" => SemanticQueryDelivery.Snapshot,
        "live" => SemanticQueryDelivery.Live,
        _ => throw DiscriminatorError(value, "query delivery")
    };

    internal static InvalidSemanticContract DiscriminatorError(string value, string description) =>
        new($"Unknown {description} discriminator '{value}'.");
}
