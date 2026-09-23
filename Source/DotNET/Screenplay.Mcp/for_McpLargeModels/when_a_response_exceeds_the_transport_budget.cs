// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_a_response_exceeds_the_transport_budget : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => McpJson.ToolResult(new { content = new string('x', McpJson.MaximumStructuredResponseBytes) }));

    [Fact] void should_refuse_instead_of_silently_truncating() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_offer_a_bounded_alternative() => _error.Message.Contains("smaller pages", StringComparison.Ordinal).ShouldBeTrue();
}
