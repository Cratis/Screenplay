// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_ConditionParser;

public class when_compiling_concept_requirement_depth
{
    [Theory]
    [InlineData(128)]
    [InlineData(129)]
    public void should_report_the_owning_diagnostic(int count)
    {
        var result = new ScreenplayCompiler().Compile("concept Label : String\n  validate\n    require " + new string('(', count) + "value == \"open\"" + new string(')', count));
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0852").ToArray();
        diagnostics.Length.ShouldEqual(count == 129 ? 1 : 0);
        if (count == 129) diagnostics[0].Location.Line.ShouldEqual(3);
    }
}
