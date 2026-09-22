// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_rejecting_unbounded_fixture_pages : Specification
{
    Exception? _negativeOffset;
    Exception? _emptyLimit;
    Exception? _excessiveLimit;

    void Because()
    {
        _negativeOffset = Catch.Exception(() => McpReadPage<int>.Create([1], _ => true, -1, 1));
        _emptyLimit = Catch.Exception(() => McpReadPage<int>.Create([1], _ => true, 0, 0));
        _excessiveLimit = Catch.Exception(() => McpReadPage<int>.Create([1], _ => true, 0, 201));
    }

    [Fact] void should_reject_a_negative_offset() => _negativeOffset.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_reject_an_empty_limit() => _emptyLimit.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_reject_an_excessive_limit() => _excessiveLimit.ShouldBeOfExactType<McpFailure>();
}
