// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_incomplete_or_repeated_fixture_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("given readmodel Row\n  second = \"b\"\n  quantity = 1\nwhen query All\nthen no result", "PLAY0351")]
    [InlineData("given readmodel Row\n  first = \"a\"\n  first = \"a\"\n  second = \"b\"\n  quantity = 1\nwhen query All\nthen no result", "PLAY0519")]
    [InlineData("when append Removed\nthen readmodel Row\n  first = \"a\"", "PLAY0351")]
    [InlineData("when append Removed\nthen no readmodel Row for {\"first\":\"a\"}", "PLAY0661")]
    [InlineData("when append Removed\nthen no readmodel Row for {\"first\":\"a\",\"second\":\"b\",\"unknown\":\"x\"}", "PLAY0662")]
    [InlineData("when append Removed\nthen no readmodel Row for {\"first\":\"a\",\"second\":\"b\",\"first\":\"a\"}", "PLAY0294")]
    [InlineData("when query All\nthen result\n  first = \"a\"", "PLAY0351")]
    public void should_report_the_fixture_diagnostic(string steps, string expected)
    {
        var result = Compile(Specification(steps));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(expected);
    }
}
