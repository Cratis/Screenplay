// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    static object? DescribeQueryShape(SemanticPerformedQueryShape? shape) => shape is null ? null : new
    {
        readModel = new
        {
            name = shape.ReadModelName,
            semanticId = shape.ReadModel?.ToString(),
            address = shape.ReadModelAddress is null ? null : McpSemanticAddresses.Describe(shape.ReadModelAddress)
        },
        authorization = DescribeQueryAuthorization(shape.Authorization),
        cardinality = shape.Cardinality.ToString(),
        delivery = shape.Delivery.ToString(),
        arguments = shape.Arguments.Select(argument => new
        {
            argument.Name,
            kind = argument.Kind.ToString(),
            type = DescribeModelType(argument.Type),
            optional = argument.IsOptional
        })
    };

    // The policies that gate a performed query, as names into the application's policies: a consumer decides what each one means (a claim, a role, anonymous access).
    static object? DescribeQueryAuthorization(SemanticAuthorization? authorization) => authorization switch
    {
        null => null,
        SemanticPolicyReference reference => new { kind = "policy", name = reference.Name },
        SemanticLogicalAuthorization logical => new { kind = logical.Operator.ToString(), left = DescribeQueryAuthorization(logical.Left), right = DescribeQueryAuthorization(logical.Right) },
        _ => new { kind = authorization.GetType().Name }
    };
}
