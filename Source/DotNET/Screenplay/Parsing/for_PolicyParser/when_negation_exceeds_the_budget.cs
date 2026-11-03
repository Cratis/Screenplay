// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PolicyParser;

public class when_negation_exceeds_the_budget
{
    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void should_bound_group_nesting(int count, bool accepted)
    {
        var text = "require " + new string('(', count) + "role \"()\"" + new string(')', count);
        var context = new ParserContext(new LineReader([new SourceLine(2, "  " + text, 2, text)]));
        var policy = PolicyParser.Parse(context, new SourceLine(1, "policy Access", 0, "policy Access"));
        (policy?.Condition is not null).ShouldEqual(accepted);
        context.Diagnostics.Count.ShouldEqual(accepted ? 0 : 1);
        if (!accepted)
        {
            context.Diagnostics[0].Code.ShouldEqual("PLAY0861");
        }
    }

    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    public void should_bound_flat_tree_depth(string operation)
    {
        foreach (var count in new[] { 128, 129 })
        {
            var text = "require " + string.Join($" {operation} ", Enumerable.Repeat("authenticated", count));
            var context = new ParserContext(new LineReader([new SourceLine(2, "  " + text, 2, text)]));
            var policy = PolicyParser.Parse(context, new SourceLine(1, "policy Access", 0, "policy Access"));
            (policy?.Condition is not null).ShouldEqual(count == 128);
            context.Diagnostics.Count.ShouldEqual(count == 128 ? 0 : 1);
        }
    }

    [Theory]
    [InlineData(127, true)]
    [InlineData(128, false)]
    public void should_bound_negation_tree_depth(int count, bool accepted)
    {
        var line = new SourceLine(1, "policy Access", 0, "policy Access");
        var text = "require " + string.Concat(Enumerable.Repeat("not ", count)) + "authenticated";
        var context = new ParserContext(new LineReader([new SourceLine(2, "  " + text, 2, text)]));
        var policy = PolicyParser.Parse(context, line);
        (policy?.Condition is not null).ShouldEqual(accepted);
        context.Diagnostics.Count.ShouldEqual(accepted ? 0 : 1);
        if (!accepted)
        {
            context.Diagnostics[0].Code.ShouldEqual("PLAY0861");
        }
    }
}
