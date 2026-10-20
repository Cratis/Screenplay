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
}
