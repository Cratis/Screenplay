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
        writer.WritePropertyName("condition");
        WritePolicyCondition(writer, policy.Condition);
        if (policy.Condition is SemanticOpaquePolicyCondition opaque)
        {
            CanonicalJson.WriteString(writer, "requirementId", opaque.RequirementId);
        }
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
