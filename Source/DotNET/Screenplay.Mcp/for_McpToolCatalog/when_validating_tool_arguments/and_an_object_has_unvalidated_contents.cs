// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_an_object_has_unvalidated_contents : given.catalog_arguments
{
    void Establish()
    {
        _tool = "semantic-diff";
        _arguments = JsonDocument.Parse("{\"before\":{\"unexpected\":42},\"after\":{}}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_only_check_the_outer_object_type() => _error.ShouldBeNull();
}
