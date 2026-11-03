// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_LogicalConditionParser;

public class when_a_flat_chain_exceeds_tree_depth
{
    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    public void should_bound_the_constructed_tree(string operation)
    {
        var context = ParserContext.ForDiagnostics();
        ConditionParser.Parse(context, string.Join($" {operation} ", Enumerable.Repeat("status == \"open\"", 128)), SourceLocation.Start).ShouldNotBeNull();
        context.Diagnostics.ShouldBeEmpty();
        ConditionParser.Parse(context, string.Join($" {operation} ", Enumerable.Repeat("status == \"open\"", 129)), SourceLocation.Start).ShouldBeNull();
        context.Diagnostics.Count.ShouldEqual(1);
        context.Diagnostics[0].Code.ShouldEqual("PLAY0861");
    }
}
