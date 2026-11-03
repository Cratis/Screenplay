// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_ConditionParser;

public class when_condition_nesting_exceeds_the_budget
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_only_the_condition_beyond_the_limit(bool strict)
    {
        var context = ParserContext.ForDiagnostics();
        var location = new SourceLocation(3, 5);
        ConditionParser.Parse(context, new string('(', 129) + "status == \"open\"" + new string(')', 129), location, strict).ShouldBeNull();
        context.Diagnostics.Count.ShouldEqual(1);
        context.Diagnostics[0].Code.ShouldEqual("PLAY0852");
        context.Diagnostics[0].Location.ShouldEqual(location);
        ConditionParser.Parse(context, new string('(', 128) + "status == \"()\"" + new string(')', 128), location, strict).ShouldNotBeNull();
        ConditionParser.Parse(context, "status == \"open\"", location, strict).ShouldNotBeNull();
        context.Diagnostics.Count.ShouldEqual(1);
    }
}
