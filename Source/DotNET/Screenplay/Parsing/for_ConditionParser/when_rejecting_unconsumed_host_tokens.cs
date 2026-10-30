// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ConditionParser;

public class when_rejecting_unconsumed_host_tokens
{
    static readonly SourceLocation _location = new(3, 5);

    [Theory]
    [InlineData("a == 1 && b == 2", false)]
    [InlineData("a == 1 && b == 2", true)]
    [InlineData("Foo.Bar(x) == 1", false)]
    [InlineData("Foo.Bar(x) == 1", true)]
    public void should_warn_once(string text, bool exact)
    {
        var context = ParserContext.ForDiagnostics();
        if (exact) context.SourceOptions = SourceOptions.Exact;
        ConditionParser.Parse(context, text, _location);
        var diagnostic = context.Diagnostics.Single(_ => _.Code == DiagnosticCodes.HostLanguageExpression);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual(_location);
    }

    [Theory]
    [InlineData("a != b")]
    [InlineData("a == 1 and (b == 2 or c == 3)")]
    [InlineData("a == \"x && y\"")]
    public void should_not_warn_for_screenplay_conditions(string text)
    {
        var context = ParserContext.ForDiagnostics();
        ConditionParser.Parse(context, text, _location);
        context.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void should_leave_strict_conditions_to_the_unsupported_operand_error()
    {
        var context = ParserContext.ForDiagnostics();
        ConditionParser.Parse(context, "a == 1 && b == 2", _location, strict: true);
        context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedActionConditionOperand);
    }
}
