// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_reading_row_narrowing_exposure : given.a_semantic_comparison
{
    const string Narrowed = """
        policy Gate
          require role "Staff"
        policy VisibleRows
          require authenticated
        agents
          tokens personal
          query Invoices.Listing.ListInvoices.ListInvoices
            scope read
            description "List visible invoices"
        module Invoices
          authorize Gate
          feature Listing
            slice StateView ListInvoices
              readmodel Invoice
                id Uuid
              query ListInvoices => Invoice[]
                narrow VisibleRows
        """;

    [Theory]
    [InlineData(false, "Unchanged")]
    [InlineData(true, "Widened")]
    void should_follow_the_row_narrowing_policy(bool changed, string expected)
    {
        CompareSnapshots(Narrowed, changed ? Narrowed.Replace("require authenticated", "require anonymous", StringComparison.Ordinal) : Narrowed);
        var result = Read(new { view = "agent-exposure", version = 2 });
        result.GetProperty("coverage").GetString().ShouldEqual("Complete");
        result.GetProperty("hasWidening").GetBoolean().ShouldEqual(changed);
        result.GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("change").GetString().ShouldEqual(expected);
    }
}
