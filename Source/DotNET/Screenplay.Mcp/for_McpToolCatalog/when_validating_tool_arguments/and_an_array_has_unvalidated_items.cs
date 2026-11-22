// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_an_array_has_unvalidated_items : given.catalog_arguments
{
    void Establish()
    {
        _tool = "propose-model-notes";
        _arguments = JsonDocument.Parse("{\"expectedRevision\":\"r\",\"expectedCatalogRevision\":\"c\",\"expectedNotesRevision\":\"n\",\"operations\":[42,null,\"unexpected\"]}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_only_check_the_outer_array_type() => _error.ShouldBeNull();
}
