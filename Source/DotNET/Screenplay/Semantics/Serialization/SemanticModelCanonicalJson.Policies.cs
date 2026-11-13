// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteCaller(Utf8JsonWriter writer, SemanticCaller caller)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("authenticated", caller.Authenticated);
        if (caller.Id is not null) CanonicalJson.WriteString(writer, "id", caller.Id);
        WriteStringArray(writer, "roles", caller.Roles);
        WriteArray(writer, "claims", caller.Claims, (json, claim) =>
        {
            json.WriteStartObject();
            CanonicalJson.WriteString(json, "type", claim.Type);
            CanonicalJson.WriteString(json, "value", claim.Value);
            json.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static void WritePolicy(Utf8JsonWriter writer, SemanticPolicy policy)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", policy.Name);
        if (!policy.Inputs.IsDefaultOrEmpty) WriteArray(writer, "inputs", policy.Inputs, WritePolicyInput);
        if (!policy.Reads.IsDefaultOrEmpty) WriteArray(writer, "reads", policy.Reads, WritePolicyRead);
        writer.WritePropertyName("condition");
        WritePolicyCondition(writer, policy.Condition);
        if (policy.Condition is SemanticOpaquePolicyCondition opaque)
        {
            CanonicalJson.WriteString(writer, "requirementId", opaque.RequirementId);
        }
        writer.WriteEndObject();
    }

    static void WritePolicyInput(Utf8JsonWriter writer, SemanticPolicyInput input)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", input.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, input.Type);
        writer.WriteEndObject();
    }

    static void WritePolicyInputMapping(Utf8JsonWriter writer, SemanticPolicyInputMapping mapping)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "input", mapping.Input);
        CanonicalJson.WriteString(writer, "property", mapping.Property);
        writer.WriteEndObject();
    }

    static void WritePolicyRead(Utf8JsonWriter writer, SemanticPolicyRead read)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", read.Name);
        writer.WriteString("readModel", read.ReadModel.ToString());
        CanonicalJson.WriteString(writer, "by", read.By);
        writer.WriteEndObject();
    }

    static void WritePolicyCondition(Utf8JsonWriter writer, SemanticPolicyCondition condition)
    {
        writer.WriteStartObject();
        switch (condition)
        {
            case SemanticOpaquePolicyCondition:
                writer.WriteString("kind", "opaque");
                break;
            case SemanticAuthenticatedCondition:
                writer.WriteString("kind", "authenticated");
                break;
            case SemanticAnonymousCondition:
                writer.WriteString("kind", "anonymous");
                break;
            case SemanticRoleCondition role:
                writer.WriteString("kind", "role");
                CanonicalJson.WriteString(writer, "role", role.Role);
                break;
            case SemanticClaimCondition claim:
                writer.WriteString("kind", "claim");
                CanonicalJson.WriteString(writer, "claim", claim.Claim);
                writer.WriteString("targetKind", claim.TargetKind switch
                {
                    SemanticClaimTargetKind.Literal => "literal",
                    SemanticClaimTargetKind.Subject => "subject",
                    SemanticClaimTargetKind.Artifact => "artifact",
                    _ => throw new InvalidSemanticContract("Unknown claim target kind.")
                });
                if (claim.Value is not null) CanonicalJson.WriteString(writer, "value", claim.Value);
                break;
            case SemanticStatePolicyCondition state:
                writer.WriteString("kind", "state");
                writer.WritePropertyName("condition");
                WriteCondition(writer, state.Condition);
                break;
            case SemanticNotPolicyCondition not:
                writer.WriteString("kind", "not");
                writer.WritePropertyName("operand");
                WritePolicyCondition(writer, not.Operand);
                break;
            case SemanticLogicalPolicyCondition logical:
                writer.WriteString("kind", "logical");
                writer.WriteString("operator", logical.Operator == SemanticLogicalOperator.And ? "and" : "or");
                writer.WritePropertyName("left");
                WritePolicyCondition(writer, logical.Left);
                writer.WritePropertyName("right");
                WritePolicyCondition(writer, logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown policy condition.");
        }
        writer.WriteEndObject();
    }

    static void WriteAuthorization(Utf8JsonWriter writer, SemanticAuthorization authorization)
    {
        writer.WriteStartObject();
        switch (authorization)
        {
            case SemanticPolicyReference reference:
                writer.WriteString("kind", "policy");
                CanonicalJson.WriteString(writer, "name", reference.Name);
                if (!reference.Mappings.IsDefaultOrEmpty) WriteArray(writer, "mappings", reference.Mappings, WritePolicyInputMapping);
                break;
            case SemanticLogicalAuthorization logical:
                writer.WriteString("kind", "logical");
                writer.WriteString("operator", logical.Operator == SemanticLogicalOperator.And ? "and" : "or");
                writer.WritePropertyName("left");
                WriteAuthorization(writer, logical.Left);
                writer.WritePropertyName("right");
                WriteAuthorization(writer, logical.Right);
                break;
            default: throw new InvalidSemanticContract("Unknown authorization node.");
        }
        writer.WriteEndObject();
    }
}
