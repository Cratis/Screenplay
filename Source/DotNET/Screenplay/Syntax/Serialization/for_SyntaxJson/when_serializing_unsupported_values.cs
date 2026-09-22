// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_serializing_unsupported_values : Specification
{
    Exception[] _errors;

    void Because()
    {
        var location = SourceLocation.Start;
        var nodes = new SyntaxNode[]
        {
            new LiteralExpressionSyntax(double.NaN, location),
            new LiteralExpressionSyntax(double.PositiveInfinity, location),
            new LiteralExpressionSyntax(float.NegativeInfinity, location),
            new LiteralExpressionSyntax(new object(), location),
            new LiteralExpressionSyntax(JsonSerializer.Deserialize<JsonElement>("12"), location),
            new IntervalTriggerSourceSyntax(1, (IntervalUnit)999, location),
            new unknown_expression(location)
        };
        _errors = [.. nodes.Select(node => Catch.Exception(() => SyntaxJson.Serialize(node)))];
    }

    [Fact] void should_cover_all_unsupported_values() => _errors.Length.ShouldEqual(7);
    [Fact] void should_reject_values_instead_of_serializing_arbitrary_clr_objects() => _errors.All(error => error is InvalidSyntaxJson).ShouldBeTrue();

    record unknown_expression(SourceLocation Location) : ExpressionSyntax(Location);
}
