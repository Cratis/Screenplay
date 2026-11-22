// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_boolean_arguments_use_both_values : given.catalog_arguments
{
    void Establish()
    {
        _tool = "find-declaration";
        _arguments = JsonDocument.Parse("{\"name\":\"Shop\",\"descendants\":true,\"includeContent\":false}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_admit_both_boolean_kinds() => _error.ShouldBeNull();
}
