// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_binding_complete_reaction_guards : Specification
{
    [Fact]
    void should_refuse_a_missing_left_operand_even_when_only_the_right_operand_is_selected() =>
        Refuse(Source("threshold Decimal", "threshold", "amount > threshold"));

    [Fact]
    void should_refuse_a_missing_right_operand_even_when_only_the_left_operand_is_selected() =>
        Refuse(Source("amount Decimal", "amount", "amount > threshold"));

    [Fact]
    void should_bind_both_operands_from_the_whole_declared_shape_regardless_of_selection()
    {
        foreach (var selected in new[] { "", "amount", "threshold", "amount\n          threshold" })
        {
            foreach (var amount in new[] { 1, 3 })
            {
                var model = given.v6_regression_models.Compile(Source("amount Decimal\n  threshold Decimal", selected, "amount > threshold", amount));
                foreach (var run in given.v6_regression_models.Runs(model))
                {
                    run.Execution.ShouldBeOfExactType<SemanticAccepted>();
                    run.Execution.World.Facts.Length.ShouldEqual(amount > 2 ? 1 : 0);
                }
            }
        }
    }

    [Fact]
    void should_not_drop_a_literal_guard_or_a_nested_compound_guard_when_no_operand_is_available()
    {
        Refuse(Source("threshold Decimal", "", "amount > 0"));
        Refuse(Source("threshold Decimal", "threshold", "(threshold > 0 and amount > threshold) or threshold < 0"));
        Refuse(Source("threshold Decimal", "", "amount > threshold").Replace("when Changed", "every 1 minutes", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_a_global_guard_that_another_trigger_cannot_resolve()
    {
        Refuse(Source("amount Decimal\n  threshold Decimal", "", "amount > threshold")
            .Replace("      event Accepted", "        every 1 minutes\n          produces Accepted\n            for \"root\"\n      event Accepted", StringComparison.Ordinal));
    }

    [Fact]
    void should_preserve_an_unreached_opaque_body_when_its_guard_is_resolvable()
    {
        var source = Source("amount Decimal\n  threshold Decimal", "", "amount > threshold", 1)
            .Replace("produces Accepted\n            for \"root\"", "```csharp\n            return [];\n            ```", StringComparison.Ordinal);
        foreach (var run in given.v6_regression_models.Runs(given.v6_regression_models.Compile(source)))
        {
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Execution.World.Facts.ShouldBeEmpty();
        }
    }

    [Fact]
    void should_refuse_unadmitted_nested_paths_read_aliases_and_mismatched_operand_types()
    {
        Refuse(Source("amount Decimal\n  threshold Decimal", "", "input.amount > threshold"));
        Refuse(Source("amount Decimal\n  threshold Decimal", "", "amount > input.threshold"));
        Refuse(Source("amount Decimal\n  threshold Decimal", "", "existing.amount > threshold"));
        Refuse(Source("amount Decimal\n  threshold String", "", "amount > threshold"));
    }

    [Fact]
    void should_bind_compound_parenthesized_conditions_and_enumeration_constants_without_guessing_properties()
    {
        var source = Source("amount Decimal\n  threshold Decimal", "", "(amount > threshold and threshold > 0) or amount < 0", 3);
        foreach (var run in given.v6_regression_models.Runs(given.v6_regression_models.Compile(source)))
        {
            run.Execution.World.Facts.Length.ShouldEqual(1);
        }

        var enumeration = Source("status Status", "", "status == sent")
            .Insert(0, "concept Status : Enum\n  sent\n  pending\n")
            .Replace("VALUES", "status = sent", StringComparison.Ordinal);
        foreach (var run in given.v6_regression_models.Runs(given.v6_regression_models.Compile(enumeration)))
        {
            run.Execution.World.Facts.Length.ShouldEqual(1);
        }
    }

    [Fact]
    void should_refuse_an_unresolvable_guard_even_on_an_unreached_opaque_trigger()
    {
        Refuse(Source("threshold Decimal", "", "amount > threshold")
            .Replace("produces Accepted\n            for \"root\"", "```csharp\n            return [];\n            ```", StringComparison.Ordinal));
    }

    static void Refuse(string source)
    {
        source = source[..source.IndexOf("      specification", StringComparison.Ordinal)];
        var parsed = new ScreenplayCompiler().Compile(source);
        parsed.Success.ShouldBeTrue();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("guard"), "guard", "Guard.play", source);
        var result = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        Assert.True(result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax), string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    static string Source(string shape, string selected, string guard, int? amount = null) => $$"""
        trigger Changed
          {{shape}}
        module Billing
          feature Guards
            slice Automation Guards
              reaction Guarded
                where {{guard}}
                when Changed
                  {{selected}}
                  produces Accepted
                    for "root"
              event Accepted
              specification Check
                when trigger Changed
                  {{(amount is null ? "VALUES" : $"amount = {amount}\n          threshold = 2")}}
                then error
        """;
}
