// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_integers_are_int32_values : given.catalog_arguments
{
    void Establish()
    {
        _tool = "read-document";
        _arguments = JsonDocument.Parse("{\"path\":\"model.play\",\"offset\":-2147483648,\"limit\":2147483647}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_admit_int32_boundaries_without_enforcing_schema_ranges() => _error.ShouldBeNull();
}
