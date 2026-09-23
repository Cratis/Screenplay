// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_parsing_a_projection_structured_value : Specification
{
    ParserContext _context = null!;
    ExpressionSyntax _expression = null!;

    void Establish() => _context = ParserContext.ForDiagnostics();
    void Because() => _expression = ExpressionParser.ParseProjectionExpression(_context, "[{\"sku\":1}]", SourceLocation.Start);

    [Fact] void should_not_emit_new_nodes_for_chronicle() => _expression.ShouldBeOfExactType<RawExpressionSyntax>();
    [Fact] void should_diagnose_the_unsupported_expression() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidExpression);
}
