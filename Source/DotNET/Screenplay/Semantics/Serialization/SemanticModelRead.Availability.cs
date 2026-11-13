// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Reads the audiences, availabilities, availability gates and provided values of ESM v53.
/// </summary>
internal static partial class SemanticModelRead
{
    internal static SemanticAudience Audience(ref Utf8JsonReader reader)
    {
        Object(ref reader, "audience");
        var seen = NewSeen();
        string? name = null;
        while (NextProperty(ref reader, seen, "audience") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                default: throw Unknown(property, "audience");
            }
        }

        Required(!string.IsNullOrEmpty(name), "audience");
        return new(name!);
    }

    internal static SemanticAvailability Availability(ref Utf8JsonReader reader)
    {
        Object(ref reader, "availability");
        var seen = NewSeen();
        string? name = null;
        ImmutableArray<SemanticAvailabilityRequirement> requirements = default;
        while (NextProperty(ref reader, seen, "availability") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "requirements": requirements = Array(ref reader, AvailabilityRequirement, property); break;
                default: throw Unknown(property, "availability");
            }
        }

        Required(!string.IsNullOrEmpty(name) && !requirements.IsDefault, "availability");
        return new(name!, requirements);
    }

    internal static SemanticAvailabilityRequirement AvailabilityRequirement(ref Utf8JsonReader reader)
    {
        Object(ref reader, "availability requirement");
        var seen = NewSeen();
        string? kind = null, audience = null, availability = null, op = null;
        SemanticAvailabilityCondition? condition = null;
        SemanticAvailabilityRequirement? left = null, right = null;
        while (NextProperty(ref reader, seen, "availability requirement") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "audience": audience = String(ref reader, property); break;
                case "availability": availability = String(ref reader, property); break;
                case "operator": op = String(ref reader, property); break;
                case "condition": RequiredToken(ref reader, JsonTokenType.StartObject, property); condition = AvailabilityCondition(ref reader); break;
                case "left": RequiredToken(ref reader, JsonTokenType.StartObject, property); left = AvailabilityRequirement(ref reader); break;
                case "right": RequiredToken(ref reader, JsonTokenType.StartObject, property); right = AvailabilityRequirement(ref reader); break;
                default: throw Unknown(property, "availability requirement");
            }
        }

        return kind switch
        {
            "audience" when !string.IsNullOrEmpty(audience) && availability is null && op is null && condition is null && left is null && right is null => new SemanticAudienceRequirement(audience),
            "availability" when !string.IsNullOrEmpty(availability) && audience is null && op is null && condition is null && left is null && right is null => new SemanticAvailabilityReference(availability),
            "condition" when condition is not null && audience is null && availability is null && op is null && left is null && right is null => new SemanticConditionRequirement(condition),
            "logical" when (op == "and" || op == "or") && left is not null && right is not null && audience is null && availability is null && condition is null =>
                new SemanticLogicalAvailability(left, op == "and" ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, right),
            _ => throw Malformed("availability requirement", "an audience, an availability, a condition or a logical requirement")
        };
    }

    internal static SemanticAvailabilityCondition AvailabilityCondition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "availability condition");
        var seen = NewSeen();
        string? kind = null, path = null, op = null;
        SemanticContextOperand? operand = null;
        SemanticAvailabilityCondition? left = null, right = null;
        while (NextProperty(ref reader, seen, "availability condition") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "path": path = String(ref reader, property); break;
                case "operator": op = String(ref reader, property); break;
                case "operand": RequiredToken(ref reader, JsonTokenType.StartObject, property); operand = ContextOperand(ref reader); break;
                case "left": RequiredToken(ref reader, JsonTokenType.StartObject, property); left = AvailabilityCondition(ref reader); break;
                case "right": RequiredToken(ref reader, JsonTokenType.StartObject, property); right = AvailabilityCondition(ref reader); break;
                default: throw Unknown(property, "availability condition");
            }
        }

        if (kind == "comparison" && !string.IsNullOrEmpty(path) && operand is not null && left is null && right is null)
        {
            var comparison = op switch
            {
                "==" => SemanticComparisonOperator.Equal,
                "!=" => SemanticComparisonOperator.NotEqual,
                ">" => SemanticComparisonOperator.GreaterThan,
                ">=" => SemanticComparisonOperator.GreaterThanOrEqual,
                "<" => SemanticComparisonOperator.LessThan,
                "<=" => SemanticComparisonOperator.LessThanOrEqual,
                _ => throw new InvalidSemanticContract("Unknown availability comparison operator.")
            };
            return new SemanticAvailabilityComparison(path, comparison, operand);
        }

        if ((kind == "and" || kind == "or") && left is not null && right is not null && path is null && op is null && operand is null)
        {
            return new SemanticLogicalAvailabilityCondition(left, kind == "and" ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, right);
        }

        throw Malformed("availability condition", "a comparison or a logical condition");
    }

    internal static SemanticContextOperand ContextOperand(ref Utf8JsonReader reader)
    {
        Object(ref reader, "context operand");
        var seen = NewSeen();
        string? path = null;
        SemanticValue? value = null;
        while (NextProperty(ref reader, seen, "context operand") is { } property)
        {
            switch (property)
            {
                case "path": path = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                default: throw Unknown(property, "context operand");
            }
        }

        Required((path is not null) != (value is not null), "context operand");
        return new(path, value);
    }

    internal static SemanticProvidedValue ProvidedValue(ref Utf8JsonReader reader)
    {
        Object(ref reader, "provided value");
        var seen = NewSeen();
        string? name = null;
        SemanticTypeReference? type = null;
        SemanticId readModel = default;
        bool optional = false, collection = false;
        SemanticProvideSource? source = null;
        while (NextProperty(ref reader, seen, "provided value") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                case "readModel": readModel = SemanticId.Parse(String(ref reader, property)); break;
                case "optional": optional = Boolean(ref reader, property); break;
                case "collection": collection = Boolean(ref reader, property); break;
                case "source": RequiredToken(ref reader, JsonTokenType.StartObject, property); source = ProvideSource(ref reader); break;
                default: throw Unknown(property, "provided value");
            }
        }

        Required(!string.IsNullOrEmpty(name) && source is not null && (type is not null) != readModel.IsSet, "provided value");
        return new(name!, source!) { Type = type, ReadModel = readModel.IsSet ? readModel : null, Optional = optional, Collection = collection };
    }

    static SemanticProvideSource ProvideSource(ref Utf8JsonReader reader)
    {
        Object(ref reader, "provided value source");
        var seen = NewSeen();
        string? kind = null;
        SemanticId query = default;
        var by = ImmutableArray<SemanticContextOperand>.Empty;
        SemanticContextOperand? value = null;
        while (NextProperty(ref reader, seen, "provided value source") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "query": query = SemanticId.Parse(String(ref reader, property)); break;
                case "by": by = Array(ref reader, ContextOperand, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = ContextOperand(ref reader); break;
                default: throw Unknown(property, "provided value source");
            }
        }

        return kind switch
        {
            "query" when query.IsSet && value is null => new SemanticQueryProvideSource(query, by),
            "host" when !query.IsSet && by.IsEmpty && value is null => new SemanticHostProvideSource(),
            "value" when !query.IsSet && by.IsEmpty && value is not null => new SemanticValueProvideSource(value),
            _ => throw Malformed("provided value source", "a query, host or value source")
        };
    }
}
