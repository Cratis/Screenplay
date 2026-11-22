// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_the_visual_tool_is_enabled : given.catalog_arguments
{
    void Establish()
    {
        _tool = McpVisualization.ToolName;
        _arguments = JsonDocument.Parse("{}");
        _visual = true;
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_admit_the_tool() => _error.ShouldBeNull();
}
