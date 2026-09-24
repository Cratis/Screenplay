// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticCaller Caller(ref Utf8JsonReader reader)
    {
        Object(ref reader, "caller");
        var seen = NewSeen();
        bool? authenticated = null;
        ImmutableArray<string> roles = default;
        ImmutableArray<SemanticCallerClaim> claims = default;
        while (NextProperty(ref reader, seen, "caller") is { } property)
        {
            switch (property)
            {
                case "authenticated": authenticated = Boolean(ref reader, property); break;
                case "roles": roles = StringArray(ref reader, property); break;
                case "claims": claims = Array(ref reader, CallerClaim, property); break;
                default: throw Unknown(property, "caller");
            }
        }
        Required(authenticated is not null && !roles.IsDefault && !claims.IsDefault, "caller");
        return new(authenticated!.Value, roles, claims);
    }

    internal static SemanticCallerClaim CallerClaim(ref Utf8JsonReader reader)
    {
        Object(ref reader, "caller claim");
        var seen = NewSeen();
        string? type = null, value = null;
        while (NextProperty(ref reader, seen, "caller claim") is { } property)
        {
            switch (property)
            {
                case "type": type = String(ref reader, property); break;
                case "value": value = String(ref reader, property); break;
                default: throw Unknown(property, "caller claim");
            }
        }
        Required(type is not null && value is not null, "caller claim");
        return new(type!, value!);
    }

    internal static SemanticPolicy Policy(ref Utf8JsonReader reader)
    {
        Object(ref reader, "policy");
        var seen = NewSeen();
        string? name = null;
        SemanticPolicyCondition? condition = null;
        while (NextProperty(ref reader, seen, "policy") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "condition": RequiredToken(ref reader, JsonTokenType.StartObject, property); condition = PolicyCondition(ref reader); break;
                default: throw Unknown(property, "policy");
            }
        }
        Required(name is not null && condition is not null, "policy");
        return new(name!, condition!);
    }

    internal static SemanticPolicyCondition PolicyCondition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "policy condition");
        var seen = NewSeen();
        string? kind = null, role = null, claim = null, targetKind = null, value = null, op = null;
        SemanticPolicyCondition? left = null, right = null;
        while (NextProperty(ref reader, seen, "policy condition") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "role": role = String(ref reader, property); break;
                case "claim": claim = String(ref reader, property); break;
                case "targetKind": targetKind = String(ref reader, property); break;
                case "value": value = String(ref reader, property); break;
                case "operator": op = String(ref reader, property); break;
                case "left": RequiredToken(ref reader, JsonTokenType.StartObject, property); left = PolicyCondition(ref reader); break;
                case "right": RequiredToken(ref reader, JsonTokenType.StartObject, property); right = PolicyCondition(ref reader); break;
                default: throw Unknown(property, "policy condition");
            }
        }
        return kind switch
        {
            "authenticated" when role is null && claim is null && targetKind is null && value is null && op is null && left is null && right is null => new SemanticAuthenticatedCondition(),
            "role" when role is not null && claim is null && targetKind is null && value is null && op is null && left is null && right is null => new SemanticRoleCondition(role),
            "claim" when claim is not null && role is null && op is null && left is null && right is null &&
                (targetKind == "subject" && value is null || (targetKind is "artifact" or "literal") && value is not null) =>
                new SemanticClaimCondition(claim, targetKind switch { "subject" => SemanticClaimTargetKind.Subject, "artifact" => SemanticClaimTargetKind.Artifact, _ => SemanticClaimTargetKind.Literal }, value),
            "logical" when (op is "and" or "or") && left is not null && right is not null && role is null && claim is null && targetKind is null && value is null =>
                new SemanticLogicalPolicyCondition(left, op == "and" ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, right),
            _ => throw Malformed("policy condition", "one exact policy condition variant")
        };
    }

    internal static SemanticAuthorization Authorization(ref Utf8JsonReader reader)
    {
        Object(ref reader, "authorization");
        var seen = NewSeen();
        string? kind = null, name = null, op = null;
        SemanticAuthorization? left = null, right = null;
        while (NextProperty(ref reader, seen, "authorization") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "name": name = String(ref reader, property); break;
                case "operator": op = String(ref reader, property); break;
                case "left": RequiredToken(ref reader, JsonTokenType.StartObject, property); left = Authorization(ref reader); break;
                case "right": RequiredToken(ref reader, JsonTokenType.StartObject, property); right = Authorization(ref reader); break;
                default: throw Unknown(property, "authorization");
            }
        }
        return kind switch
        {
            "policy" when name is not null && op is null && left is null && right is null => new SemanticPolicyReference(name),
            "logical" when name is null && (op is "and" or "or") && left is not null && right is not null =>
                new SemanticLogicalAuthorization(left, op == "and" ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or, right),
            _ => throw Malformed("authorization", "a policy reference or logical authorization")
        };
    }
}
