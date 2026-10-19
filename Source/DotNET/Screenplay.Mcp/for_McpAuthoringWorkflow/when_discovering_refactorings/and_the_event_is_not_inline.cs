// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_the_event_is_not_inline : given.a_refactoring_workspace
{
    string[] _extracted = [];

    void Because() => _extracted = [.. Offers("extract-inline-event").Select(offer => offer.GetProperty("name").GetString()!)];

    [Fact] void should_not_offer_extraction() => _extracted.ShouldNotContain("ProjectArchived");
}
