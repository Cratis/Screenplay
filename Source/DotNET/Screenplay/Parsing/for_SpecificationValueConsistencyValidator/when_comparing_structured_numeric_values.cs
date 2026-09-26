// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SpecificationValueConsistencyValidator;

public class when_comparing_structured_numeric_values
{
    [Fact]
    public void should_compare_equivalent_numeric_kinds_in_a_list()
    {
        var (integer, other) = Values(2L, 2.0m);
        SpecificationValueConsistencyValidator.Equal(integer, other).ShouldBeTrue();
    }

    [Fact]
    public void should_not_round_binary_fractions_in_a_list()
    {
        var (floating, precise) = Values(0.1d, 0.1m);
        SpecificationValueConsistencyValidator.Equal(floating, precise).ShouldBeFalse();
    }

    static (object? First, object? Second) Values(object first, object second)
    {
        var location = SourceLocation.Start;
        var application = new ScreenplayCompiler().Parse("module Orders\n  feature Placement\n    slice StateChange Place\n").Value!;
        var declarations = new ConsistencyDeclarations(application, []);
        var type = new TypeRefSyntax("Decimal", true, false, location);
        SpecificationValueConsistencyValidator.TryValue(
            new ListExpressionSyntax([new LiteralExpressionSyntax(first, location)], location), type, declarations, out var firstValue).ShouldBeTrue();
        SpecificationValueConsistencyValidator.TryValue(
            new ListExpressionSyntax([new LiteralExpressionSyntax(second, location)], location), type, declarations, out var secondValue).ShouldBeTrue();
        return (firstValue, secondValue);
    }
}
