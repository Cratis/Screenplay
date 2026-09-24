// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelCanonicalJson
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
        if (operand.Property.IsSet)
        {
            writer.WriteString("property", operand.Property.ToString());
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
    internal static SemanticCondition Condition(ref Utf8JsonReader reader)
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
                    if (kind == "and" || kind == "or") logicalLeft = Condition(ref reader);
                    else comparisonLeft = ConditionOperand(ref reader);
                    break;
                case "right":
                    RequiredToken(ref reader, JsonTokenType.StartObject, property);
                    if (kind == "and" || kind == "or") logicalRight = Condition(ref reader);
                    else comparisonRight = ConditionOperand(ref reader);
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

    internal static SemanticConditionOperand ConditionOperand(ref Utf8JsonReader reader)
    {
        Object(ref reader, "condition operand");
        var seen = NewSeen();
        SemanticId propertyId = default;
        SemanticValue? value = null;
        var valueRead = false;
        while (NextProperty(ref reader, seen, "condition operand") is { } property)
        {
            switch (property)
            {
                case "property": propertyId = SemanticId.Parse(String(ref reader, property)); break;
                case "value": valueRead = true; RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                default: throw Unknown(property, "condition operand");
            }
        }

        Required(propertyId.IsSet != valueRead, "condition operand");
        return new(propertyId, value);
    }

    internal static SemanticRequirement Requirement(ref Utf8JsonReader reader)
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
                case "condition": RequiredToken(ref reader, JsonTokenType.StartObject, property); condition = Condition(ref reader); break;
                case "message": messageRead = true; message = NullableString(ref reader, property); break;
                case "severity": severity = ParseSeverity(String(ref reader, property)); break;
                default: throw Unknown(property, "requirement");
            }
        }

        Required(condition is not null && messageRead, "requirement");
        return new(condition!, message) { Severity = severity };
    }
}
