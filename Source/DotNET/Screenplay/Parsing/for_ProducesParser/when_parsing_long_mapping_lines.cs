// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_ProducesParser;

public class when_parsing_long_mapping_lines : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_a_long_mapping_without_a_value(bool hasEquals)
    {
        var mapping = hasEquals ? new string('a', 100_000) + " = " : string.Concat(Enumerable.Repeat("a.", 50_000));
        var parsed = new ScreenplayCompiler().Parse(Source("produces Recorded", mapping));
        parsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.InvalidPropertyMapping);
    }

    [Fact]
    void should_parse_a_long_typed_mapping_without_a_timeout()
    {
        var name = new string('a', 100_000);
        var parsed = new ScreenplayCompiler().Parse(Source("produces event Recorded", name + " String = value"));
        parsed.Diagnostics.ShouldBeEmpty();
        var production = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
        production.Mappings.Single().Property.ShouldEqual(name);
        production.InlineEvent!.Properties.Single().Name.ShouldEqual(name);
    }

    static string Source(string production, string mapping) => "module Banking\n  feature Posting\n    slice StateChange Record\n      command Record\n        " + production + "\n          " + mapping;
}
