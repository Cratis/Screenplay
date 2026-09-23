// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_paging_before_projecting_response_items : Specification
{
    int _projected;
    McpPage<int> _page = null!;

    void Because() => _page = McpPaging.Page(
        Enumerable.Range(0, 10000),
        value =>
        {
            _projected++;
            return value;
        },
        JsonSerializer.SerializeToElement(new { offset = 500, limit = 1 }),
        "snapshot");

    [Fact] void should_count_all_subjects() => _page.TotalCount.ShouldEqual(10000);
    [Fact] void should_project_only_the_requested_item() => _projected.ShouldEqual(1);
    [Fact] void should_return_the_selected_item() => _page.Items.Single().ShouldEqual(500);
    [Fact] void should_offer_exact_continuation() => _page.NextOffset.ShouldEqual(501);
}
