// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson.given;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

// The kinds are discovered by reflection, so an expression kind added without a printer arm fails here
// instead of printing as an empty string.
public class when_printing_every_expression_kind : Specification
{
    ExpressionSyntax[] _expressions;
    Exception? _error;
    string[] _printed = [];

    void Establish() => _expressions = [.. syntax_examples.Types
        .Where(typeof(ExpressionSyntax).IsAssignableFrom)
        .Select(type => (ExpressionSyntax)syntax_examples.Create(type))];

    void Because() => _error = Catch.Exception(() => _printed = [.. _expressions.Select(ScreenplaySyntaxText.Expression)]);

    [Fact] void should_discover_the_expression_kinds() => _expressions.Length.ShouldBeGreaterThan(10);
    [Fact] void should_print_every_kind() => _error.ShouldBeNull();
    [Fact] void should_print_every_kind_as_text() => _printed.All(text => text.Length > 0).ShouldBeTrue();
}
