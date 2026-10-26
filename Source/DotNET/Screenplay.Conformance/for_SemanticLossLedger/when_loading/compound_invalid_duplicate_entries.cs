// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class compound_invalid_duplicate_entries : given.a_ledger
{
    void Because()
    {
        var entry = $$"""{"fixture":"{{Fixture}}","change":"missing","path":"/*","diagnostics":[],"issue":"bad","byDesign":" "}""";
        _ledger = Load($"{entry}, {entry}");
    }

    [Fact]
    void should_accumulate_shape_problems_before_the_duplicate_finding() => Assert.Equal(
    new[]
    {
        $"Loss 0 ({Fixture}): the entry must name a direction, 'render' or 'adapter'.",
        $"Loss 0 ({Fixture}): the path must not contain a wildcard.",
        $"Loss 0 ({Fixture}): the entry must carry exactly one of 'issue' and 'byDesign'.",
        $"Loss 0 ({Fixture}): the issue 'bad' is not of the form 'Owner/Repo#N'.",
        $"Loss 0 ({Fixture}): 'byDesign' must give a reason.",
        $"Loss 1 ({Fixture}): the entry must name a direction, 'render' or 'adapter'.",
        $"Loss 1 ({Fixture}): the path must not contain a wildcard.",
        $"Loss 1 ({Fixture}): the entry must carry exactly one of 'issue' and 'byDesign'.",
        $"Loss 1 ({Fixture}): the issue 'bad' is not of the form 'Owner/Repo#N'.",
        $"Loss 1 ({Fixture}): 'byDesign' must give a reason.",
        $"Loss 1 ({Fixture}): the entry duplicates another entry for the same fixture, change and path.",
    },
    _ledger.Problems);
}
