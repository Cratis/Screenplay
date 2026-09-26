// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_numeric_spellings_describe_the_same_outcome : given.a_compiler
{
    [Theory]
    [InlineData("2", "2.0", "")]
    [InlineData("2e0", "2", "")]
    [InlineData("2", "2.0", " when amount == 2.0")]
    [InlineData("2.0", "2", " when amount != 3e0")]
    public void should_compile_value_equivalent_mappings_and_conditions(string stated, string expected, string condition)
    {
        var producer = condition.Length == 0
            ? $"produces OrderPlaced\n          amount = {stated}"
            : $"produces{condition}\n          OrderPlaced\n            amount = {stated}";
        var source = $"module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        {producer}\n      event OrderPlaced\n        amount Decimal\n      specification CanPlace\n        when PlaceOrder\n          amount = {stated}\n        then OrderPlaced\n          amount = {expected}\n";
        var result = _compiler.Compile(source);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeFalse();
    }

    [Theory]
    [InlineData("2", "2.0", "!=")]
    [InlineData("2.0", "2", "!=")]
    [InlineData("2", "2e0", "==")]
    public void should_consider_numeric_conditions_by_value(string stated, string compared, string comparison)
    {
        var source = $"module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        produces when amount {comparison} {compared}\n          OrderPlaced\n            amount = amount\n      event OrderPlaced\n        amount Decimal\n      specification CannotPlace\n        when PlaceOrder\n          amount = {stated}\n        then OrderPlaced\n          amount = 100\n";
        var result = _compiler.Compile(source);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
    }
}
