// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_AuthorizeParser;

public class when_group_nesting_exceeds_the_budget
{
    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    public void should_bound_the_requirement_tree(string operation)
    {
        var context = ParserContext.ForDiagnostics();
        var accepted = "authorize " + string.Join($" {operation} ", Enumerable.Repeat("Access", 128));
        AuthorizeParser.Parse(context, new SourceLine(7, accepted, 0, accepted)).ShouldNotBeNull();
        context.Diagnostics.ShouldBeEmpty();
        var refused = accepted + $" {operation} Access";
        AuthorizeParser.Parse(context, new SourceLine(7, refused, 0, refused)).ShouldBeNull();
        context.Diagnostics.Count.ShouldEqual(1);
        context.Diagnostics[0].Code.ShouldEqual("PLAY0852");
        const string later = "authorize Access";
        AuthorizeParser.Parse(context, new SourceLine(8, later, 0, later)).ShouldNotBeNull();
        context.Diagnostics.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void should_bound_each_requirement(int count, bool accepted)
    {
        var context = ParserContext.ForDiagnostics();
        var text = "authorize " + new string('(', count) + "Access" + new string(')', count);
        var result = AuthorizeParser.Parse(context, new SourceLine(7, text, 0, text));
        (result is not null).ShouldEqual(accepted);
        context.Diagnostics.Count.ShouldEqual(accepted ? 0 : 1);
        if (!accepted)
        {
            context.Diagnostics[0].Code.ShouldEqual("PLAY0852");
            context.Diagnostics[0].Location.Line.ShouldEqual(7);
        }
    }
}
