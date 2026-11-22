// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_an_enum_value_is_supported : given.catalog_arguments
{
    void Establish()
    {
        _tool = "dependency-graph";
        _arguments = JsonDocument.Parse("{\"view\":\"edges\"}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_admit_the_enum_value() => _error.ShouldBeNull();
}
