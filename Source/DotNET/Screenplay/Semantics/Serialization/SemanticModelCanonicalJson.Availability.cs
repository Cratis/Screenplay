// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the audiences, availabilities, availability gates and provided values of ESM v53.
/// </summary>
public static partial class SemanticModelCanonicalJson
{
    static void WriteAvailable(Utf8JsonWriter writer, ImmutableArray<SemanticAvailabilityRequirement> gates, SemanticVersion version)
    {
        if (gates.IsDefaultOrEmpty) return;
        RequireV53(version, "Availability gates");
        WriteArray(writer, "available", gates, WriteAvailabilityRequirement);
    }

    static void WriteProvides(Utf8JsonWriter writer, ImmutableArray<SemanticProvidedValue> provides, SemanticVersion version)
    {
        if (provides.IsDefaultOrEmpty) return;
        RequireV53(version, "Provided values");
        WriteArray(writer, "provides", provides, WriteProvidedValue);
    }

    static void WriteAudiencesAndAvailabilities(Utf8JsonWriter writer, SemanticApplication application, SemanticVersion version)
    {
        if (!application.Audiences.IsDefaultOrEmpty)
        {
            RequireV53(version, "Audiences");
            WriteArray(writer, "audiences", application.Audiences.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteAudience);
        }

        if (!application.Availabilities.IsDefaultOrEmpty)
        {
            RequireV53(version, "Availabilities");
            WriteArray(writer, "availabilities", application.Availabilities.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteAvailability);
        }

        WriteProvides(writer, application.Provides, version);
    }

    static void RequireV53(SemanticVersion version, string what)
    {
        if (version.Major < 53) throw new InvalidSemanticContract($"{what} require ESM v53 or later.");
    }

    static void WriteAudience(Utf8JsonWriter writer, SemanticAudience audience)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", audience.Name);
        writer.WriteEndObject();
    }

    static void WriteAvailability(Utf8JsonWriter writer, SemanticAvailability availability)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", availability.Name);
        WriteArray(writer, "requirements", availability.Requirements, WriteAvailabilityRequirement);
        writer.WriteEndObject();
    }

    static void WriteAvailabilityRequirement(Utf8JsonWriter writer, SemanticAvailabilityRequirement requirement)
    {
        writer.WriteStartObject();
        switch (requirement)
        {
            case SemanticAudienceRequirement audience:
                writer.WriteString("kind", "audience");
                CanonicalJson.WriteString(writer, "audience", audience.Audience);
                break;
            case SemanticAvailabilityReference reference:
                writer.WriteString("kind", "availability");
                CanonicalJson.WriteString(writer, "availability", reference.Availability);
                break;
            case SemanticConditionRequirement condition:
                writer.WriteString("kind", "condition");
                writer.WritePropertyName("condition");
                WriteAvailabilityCondition(writer, condition.Condition);
                break;
            case SemanticLogicalAvailability logical:
                writer.WriteString("kind", "logical");
                writer.WriteString("operator", LogicalName(logical.Operator));
                writer.WritePropertyName("left");
                WriteAvailabilityRequirement(writer, logical.Left);
                writer.WritePropertyName("right");
                WriteAvailabilityRequirement(writer, logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown availability requirement.");
        }

        writer.WriteEndObject();
    }

    static void WriteAvailabilityCondition(Utf8JsonWriter writer, SemanticAvailabilityCondition condition)
    {
        writer.WriteStartObject();
        switch (condition)
        {
            case SemanticAvailabilityComparison comparison:
                writer.WriteString("kind", "comparison");
                CanonicalJson.WriteString(writer, "path", comparison.Path);
                writer.WriteString("operator", ComparisonName(comparison.Operator));
                writer.WritePropertyName("operand");
                WriteContextOperand(writer, comparison.Operand);
                break;
            case SemanticLogicalAvailabilityCondition logical:
                writer.WriteString("kind", LogicalName(logical.Operator));
                writer.WritePropertyName("left");
                WriteAvailabilityCondition(writer, logical.Left);
                writer.WritePropertyName("right");
                WriteAvailabilityCondition(writer, logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown availability condition.");
        }

        writer.WriteEndObject();
    }

    static void WriteContextOperand(Utf8JsonWriter writer, SemanticContextOperand operand)
    {
        writer.WriteStartObject();
        if (operand.Path is not null) CanonicalJson.WriteString(writer, "path", operand.Path);
        if (operand.Value is not null)
        {
            writer.WritePropertyName("value");
            WriteValue(writer, operand.Value);
        }

        writer.WriteEndObject();
    }

    static void WriteProvidedValue(Utf8JsonWriter writer, SemanticProvidedValue provided)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", provided.Name);
        if (provided.Type is not null)
        {
            writer.WritePropertyName("type");
            WriteTypeReference(writer, provided.Type);
        }

        if (provided.ReadModel is { } readModel) writer.WriteString("readModel", readModel.ToString());
        if (provided.Optional) writer.WriteBoolean("optional", true);
        if (provided.Collection) writer.WriteBoolean("collection", true);
        writer.WritePropertyName("source");
        writer.WriteStartObject();
        switch (provided.Source)
        {
            case SemanticQueryProvideSource query:
                writer.WriteString("kind", "query");
                writer.WriteString("query", query.Query.ToString());
                if (!query.By.IsDefaultOrEmpty) WriteArray(writer, "by", query.By, WriteContextOperand);
                break;
            case SemanticHostProvideSource:
                writer.WriteString("kind", "host");
                break;
            case SemanticValueProvideSource value:
                writer.WriteString("kind", "value");
                writer.WritePropertyName("value");
                WriteContextOperand(writer, value.Value);
                break;
            default: throw new InvalidSemanticContract("Unknown provided value source.");
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static string LogicalName(SemanticLogicalOperator @operator) => @operator switch
    {
        SemanticLogicalOperator.And => "and",
        SemanticLogicalOperator.Or => "or",
        _ => throw new InvalidSemanticContract("Unknown logical operator.")
    };

    static string ComparisonName(SemanticComparisonOperator @operator) => @operator switch
    {
        SemanticComparisonOperator.Equal => "==",
        SemanticComparisonOperator.NotEqual => "!=",
        SemanticComparisonOperator.GreaterThan => ">",
        SemanticComparisonOperator.GreaterThanOrEqual => ">=",
        SemanticComparisonOperator.LessThan => "<",
        SemanticComparisonOperator.LessThanOrEqual => "<=",
        _ => throw new InvalidSemanticContract("An availability condition compares with ==, !=, >, >=, < or <=.")
    };
}
