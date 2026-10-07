// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ConditionParser;

public class when_preserving_legacy_condition_tokenization
{
    [Theory]
    [InlineData("status == [\"open\"]")]
    [InlineData("status == \"open\";")]
    [InlineData("status == {\"open\"")]
    public void should_keep_existing_non_guard_condition_behavior(string text)
    {
        var context = ParserContext.ForDiagnostics();
        var condition = ConditionParser.Parse(context, text, SourceLocation.Start);
        context.Diagnostics.ShouldBeEmpty();
        condition.ShouldEqual(new ComparisonConditionSyntax("status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", SourceLocation.Start), SourceLocation.Start));
    }
}
