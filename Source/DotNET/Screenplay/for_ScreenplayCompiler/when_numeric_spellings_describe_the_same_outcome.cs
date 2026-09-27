// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

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

    [Fact]
    public void should_compile_mixed_ast_authored_double_and_hand_typed_decimal_values()
    {
        // Plain typed JSON 0.1 is a Double; the hand-typed specification literal is a Decimal.
        var application = _compiler.Parse("module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        produces OrderPlaced\n          amount = 0.1\n      event OrderPlaced\n        amount Decimal\n      specification CanPlace\n        when PlaceOrder\n          amount = 0.1\n        then OrderPlaced\n          amount = 0.1\n").Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var produce = command.Produces.Single();
        var mapping = produce.Mappings.Single();
        var edited = application with
        {
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with
                    {
                        Commands = [command with
                        {
                            Produces = [produce with
                            {
                                Mappings = [mapping with { Source = new LiteralExpressionSyntax(0.1d, SourceLocation.Start) }]
                            }]
                        }]
                    }]
                }]
            }]
        };
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(edited, context);
        Assert.DoesNotContain(context.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome);
        var printed = new Printing.ScreenplayPrinter(authoring: true).Print(edited);
        Assert.Contains("amount = 0.1", printed, StringComparison.Ordinal);
        var result = _compiler.Compile(printed);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    public void should_distinguish_authored_values_that_main_rounded_to_one_double()
    {
        const string source = "module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        produces OrderPlaced\n          amount = amount\n      event OrderPlaced\n        amount Decimal\n      specification CanPlace\n        when PlaceOrder\n          amount = 100000000000000020\n        then OrderPlaced\n          amount = 100000000000000016\n";
        var result = _compiler.Compile(source);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
    }

    [Theory]
    [InlineData("2", "2.0", "!=")]
    [InlineData("2.0", "2", "!=")]
    public void should_consider_numeric_conditions_by_value(string stated, string compared, string comparison)
    {
        var source = $"module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        produces when amount {comparison} {compared}\n          OrderPlaced\n            amount = amount\n      event OrderPlaced\n        amount Decimal\n      specification CannotPlace\n        when PlaceOrder\n          amount = {stated}\n        then OrderPlaced\n          amount = {stated}\n";
        var result = _compiler.Compile(source);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
    }
}
