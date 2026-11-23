// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_an_agent_exposure_finding_is_oversized : given.an_agent_exposure_comparison
{
    Exception _failure;

    void Because()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents.Replace("List projects", new string('ø', 40_000), StringComparison.Ordinal));
        _failure = Catch.Exception(() => ReadExposure())!;
    }

    [Fact] void should_refuse_without_truncation() => _failure.Message.ShouldContain("ResponseTooLarge");
}
