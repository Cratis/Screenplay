// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_rejecting_host_language_mapping_sources
{
    static readonly SourceLocation _location = new(4, 7);

    [Theory]
    [InlineData("Price && Qty")]
    [InlineData("a || b")]
    [InlineData("Math.Round(Price)")]
    [InlineData("calculate(x)")]
    public void should_warn_once_and_keep_the_raw_expression(string text)
    {
        var context = ParserContext.ForDiagnostics();
        var expression = ExpressionParser.ParseMappingSource(context, text, _location);
        var diagnostic = context.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.HostLanguageExpression);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual(_location);
        expression.ShouldEqual(new RawExpressionSyntax(text, _location));
    }

    [Theory]
    [InlineData("\"a && b\"")]
    [InlineData("'f(x)'")]
    [InlineData("{\"k\":\"a || b\"}")]
    [InlineData("a + 1")]
    [InlineData("Price")]
    public void should_not_warn_for_admitted_text(string text)
    {
        var context = ParserContext.ForDiagnostics();
        ExpressionParser.ParseMappingSource(context, text, _location);
        context.Diagnostics.Where(_ => _.Code == DiagnosticCodes.HostLanguageExpression).ShouldBeEmpty();
    }
}
