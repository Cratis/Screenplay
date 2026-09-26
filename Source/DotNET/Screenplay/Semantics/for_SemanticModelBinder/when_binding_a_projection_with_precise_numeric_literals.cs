// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_projection_with_precise_numeric_literals : given.a_semantic_binder
{
    [Theory]
    [InlineData("9007199254740993", "9007199254740993")]
    [InlineData("9223372036854775809", "9223372036854775809")]
    [InlineData("1e-3", "0.001")]
    [InlineData("2.5E+4", "25000")]
    public void should_keep_the_value_in_the_projection_and_canonical_esm(string literal, string expected)
    {
        var result = Bind(Source(literal));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var projection = result.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(slice => slice.Projections).Single();
        var number = (SemanticNumberValue)((SemanticValueExpression)projection.Transitions.Single().Mappings
            .Single(mapping => mapping.Source is SemanticValueExpression).Source).Value;
        number.Value.ShouldEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        var serialized = System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(result.Value.Model));
        serialized.ShouldContain($"\"value\":{expected}");
    }

    [Theory]
    [InlineData("0.123456789012345678901234567890")]
    [InlineData("18446744073709551616.000000000000000000000000000001")]
    public void should_keep_fallback_double_canonical_bytes_after_printing(string literal)
    {
        var original = Source(literal);
        var syntax = new ScreenplayCompiler().Parse(original).Value!;
        var printed = new ScreenplayPrinter().Print(syntax);
        var before = Bind(original);
        var after = Bind(printed);
        Assert.True(before.Success, string.Join("; ", before.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(after.Success, string.Join("; ", after.Diagnostics.Select(diagnostic => diagnostic.Message)));
        SemanticModelSerializer.Serialize(after.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(before.Value!.Model));
    }

    [Theory]
    [InlineData("1e100")]
    public void should_reject_a_double_outside_the_esm_decimal_range(string literal)
    {
        var result = Bind(Source(literal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax &&
            diagnostic.Message.Contains("numeric literal", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("0.00000000000000000000000000001")]
    [InlineData("1e-29")]
    public void should_keep_main_esm_underflow_behavior(string literal)
    {
        var result = Bind(Source(literal));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var projection = result.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(slice => slice.Projections).Single();
        var number = (SemanticNumberValue)((SemanticValueExpression)projection.Transitions.Single().Mappings
            .Single(mapping => mapping.Source is SemanticValueExpression).Source).Value;
        number.Value.ShouldEqual(0m);
    }

    static string Source(string literal) =>
        $"module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        id String\n    slice StateView Lookup\n      readmodel Summary\n        id String\n        quantity Decimal\n      query SummaryById => Summary?\n        by id String\n      projection SummaryProjection => Summary\n        from Registered key id\n          quantity = {literal}\n";
}
