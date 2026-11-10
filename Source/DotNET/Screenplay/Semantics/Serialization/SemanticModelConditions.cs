// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteCondition(Utf8JsonWriter writer, SemanticCondition condition)
    {
        writer.WriteStartObject();
        switch (condition)
        {
            case SemanticComparison comparison:
                writer.WriteString("kind", "comparison");
                WriteConditionOperand(writer, "left", comparison.Left);
                writer.WriteString("operator", comparison.Operator switch
                {
                    SemanticComparisonOperator.Equal => "==", SemanticComparisonOperator.NotEqual => "!=",
                    SemanticComparisonOperator.GreaterThan => ">", SemanticComparisonOperator.GreaterThanOrEqual => ">=",
                    SemanticComparisonOperator.LessThan => "<", SemanticComparisonOperator.LessThanOrEqual => "<=",
                    SemanticComparisonOperator.Contains => "contains", SemanticComparisonOperator.StartsWith => "startsWith",
                    _ => throw new InvalidSemanticContract("Unknown condition comparison operator.")
                });
                WriteConditionOperand(writer, "right", comparison.Right);
                break;
            case SemanticLogicalCondition logical:
                writer.WriteString("kind", logical.Operator switch
                {
                    SemanticLogicalOperator.And => "and", SemanticLogicalOperator.Or => "or",
                    _ => throw new InvalidSemanticContract("Unknown logical operator.")
                });
                writer.WritePropertyName("left");
                WriteCondition(writer, logical.Left);
                writer.WritePropertyName("right");
                WriteCondition(writer, logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown condition node.");
        }

        writer.WriteEndObject();
    }

    static void WriteConditionOperand(Utf8JsonWriter writer, string name, SemanticConditionOperand operand)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        if (operand.Clock is not null)
        {
            WriteClockOperand(writer, operand.Clock);
        }
        else if (operand.CallerId)
        {
            writer.WriteBoolean("callerId", true);
        }
        else if (operand.Property.IsSet)
        {
            if (operand.Read is not null) CanonicalJson.WriteString(writer, "read", operand.Read);
            writer.WriteString("property", operand.Property.ToString());
            if (operand.DateOf) writer.WriteBoolean("dateOf", true);
        }
        else
        {
            writer.WritePropertyName("value");
            WriteValue(writer, operand.Value!);
        }

        writer.WriteEndObject();
    }

    static void WriteRequirement(Utf8JsonWriter writer, SemanticRequirement requirement)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("condition");
        WriteCondition(writer, requirement.Condition);
        WriteOptionalString(writer, "message", requirement.Message);
        WriteSeverity(writer, requirement.Severity);
        writer.WriteEndObject();
    }
}

internal static partial class SemanticModelRead
{
    internal static SemanticCondition Condition(ref Utf8JsonReader reader) => Condition(ref reader, 0);

    internal static SemanticCondition Condition(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "condition");
        var seen = NewSeen();
        string? kind = null;
        SemanticConditionOperand? comparisonLeft = null;
        SemanticConditionOperand? comparisonRight = null;
        SemanticCondition? logicalLeft = null;
        SemanticCondition? logicalRight = null;
        string? op = null;
        while (NextProperty(ref reader, seen, "condition") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "operator": op = String(ref reader, property); break;
                case "left":
                    RequiredToken(ref reader, JsonTokenType.StartObject, property);
                    if (kind == "and" || kind == "or") logicalLeft = Condition(ref reader, schemaVersion);
                    else comparisonLeft = ConditionOperand(ref reader, schemaVersion);
                    break;
                case "right":
                    RequiredToken(ref reader, JsonTokenType.StartObject, property);
                    if (kind == "and" || kind == "or") logicalRight = Condition(ref reader, schemaVersion);
                    else comparisonRight = ConditionOperand(ref reader, schemaVersion);
                    break;
                default: throw Unknown(property, "condition");
            }
        }

        if (kind == "comparison" && comparisonLeft is not null && comparisonRight is not null)
        {
            var comparison = op switch
            {
                "==" => SemanticComparisonOperator.Equal, "!=" => SemanticComparisonOperator.NotEqual,
                ">" => SemanticComparisonOperator.GreaterThan, ">=" => SemanticComparisonOperator.GreaterThanOrEqual,
                "<" => SemanticComparisonOperator.LessThan, "<=" => SemanticComparisonOperator.LessThanOrEqual,
                "contains" when schemaVersion >= LanguageVersion.V45.Major => SemanticComparisonOperator.Contains,
                "startsWith" when schemaVersion >= LanguageVersion.V45.Major => SemanticComparisonOperator.StartsWith,
                _ => throw new InvalidSemanticContract("Unknown condition comparison operator.")
            };
            return new SemanticComparison(comparisonLeft, comparison, comparisonRight);
        }

        if ((kind == "and" || kind == "or") && logicalLeft is not null && logicalRight is not null && op is null)
        {
            return new SemanticLogicalCondition(logicalLeft, kind == "and" ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, logicalRight);
        }

        throw new InvalidSemanticContract("Condition shape is invalid or out of order.");
    }

    internal static SemanticConditionOperand ConditionOperand(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "condition operand");
        var seen = NewSeen();
        SemanticId propertyId = default;
        SemanticValue? value = null;
        var valueRead = false;
        string? read = null;
        SemanticClockOperand? clock = null;
        var dateOf = false;
        var callerId = false;
        while (NextProperty(ref reader, seen, "condition operand") is { } property)
        {
            switch (property)
            {
                case "read" when schemaVersion >= LanguageVersion.V25.Major: read = String(ref reader, property); break;
                case "clock" when schemaVersion >= LanguageVersion.V37.Major: clock = Nested(ref reader, ClockOperand, property); break;
                case "dateOf" when schemaVersion >= LanguageVersion.V37.Major: dateOf = OnlyTrue(ref reader, property, "condition operand"); break;
                case "callerId" when schemaVersion >= LanguageVersion.V49.Major: callerId = OnlyTrue(ref reader, property, "condition operand"); break;
                case "property": propertyId = SemanticId.Parse(String(ref reader, property)); break;
                case "value": valueRead = true; RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                default: throw Unknown(property, "condition operand");
            }
        }

        var fields = new ConditionOperandFields(propertyId, value, valueRead, read, dateOf);
        if (callerId)
        {
            Required(!propertyId.IsSet && !valueRead && read is null && !dateOf && clock is null, "condition operand");
            return new(default, null) { CallerId = true };
        }

        return clock is null ? fields.PropertyOperand() : fields.ClockOperand(clock);
    }

    internal static SemanticRequirement Requirement(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "requirement");
        var seen = NewSeen();
        SemanticCondition? condition = null;
        string? message = null;
        var messageRead = false;
        var severity = SemanticValidationSeverity.Error;
        while (NextProperty(ref reader, seen, "requirement") is { } property)
        {
            switch (property)
            {
                case "condition": RequiredToken(ref reader, JsonTokenType.StartObject, property); condition = Condition(ref reader, schemaVersion); break;
                case "message": messageRead = true; message = NullableString(ref reader, property); break;
                case "severity": severity = ParseSeverity(String(ref reader, property)); break;
                default: throw Unknown(property, "requirement");
            }
        }

        Required(condition is not null && messageRead, "requirement");
        return new(condition!, message) { Severity = severity };
    }

    readonly record struct ConditionOperandFields(SemanticId PropertyId, SemanticValue? Value, bool ValueRead, string? Read, bool DateOf)
    {
        // A clock operand stands alone: it carries no property, value, read or date of.
        public SemanticConditionOperand ClockOperand(SemanticClockOperand clock)
        {
            Required(!PropertyId.IsSet && !ValueRead && Read is null && !DateOf, "condition operand");
            return new(default, null) { Clock = clock };
        }

        public SemanticConditionOperand PropertyOperand()
        {
            Required(PropertyId.IsSet != ValueRead && (Read is null || PropertyId.IsSet) && (!DateOf || PropertyId.IsSet), "condition operand");
            return new(PropertyId, Value) { Read = Read, DateOf = DateOf };
        }
    }
}
