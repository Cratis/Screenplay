// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_enforcing_generation_budgets
{
    [Fact]
    void should_allow_exactly_the_operation_budget_and_refuse_the_next_unit()
    {
        var budget = new GenerationWorkBudget();
        budget.Spend(GenerationWorkBudget.OperationLimit);
        budget.Used.ShouldEqual(100_000_000L);
        Catch.Exception(() => budget.Spend()).ShouldBeOfExactType<GenerationSupportBudgetExceeded>();
        budget.Used.ShouldEqual(100_000_000L);
    }

    [Fact]
    void should_check_character_positions_before_allocation()
    {
        var pattern = new GenerationPattern([new("AB", Maximum: 2048)], null);
        var automaton = new GenerationPatternAutomaton(pattern, null, false, new());
        automaton.IsEmpty(automaton.Start).ShouldBeFalse();
        Catch.Exception(() => _ = new GenerationPatternAutomaton(pattern with { Tokens = [new("AB", Maximum: 2049)] }, null, false, new())).ShouldBeOfExactType<GenerationSupportBudgetExceeded>();
    }

    [Fact]
    void should_refuse_an_exhausted_checksum_product_as_a_hard_error()
    {
        var pattern = GenerationPattern.Parse("*{64}*{64}*{64}*{63}", "mod11");
        var error = Catch.Exception(() => GenerationPatternSupport.Count(pattern));
        error.ShouldBeOfExactType<GenerationSupportBudgetExceeded>();
        error.Message.ShouldContain("10000 distinct DFA/product states", StringComparison.Ordinal);
    }

    [Fact]
    void should_include_appended_check_digits_in_the_final_length_bound()
    {
        GenerationPattern.Parse("#{64}#{64}#{64}#{64}").MaximumLength.ShouldEqual(256);
        GenerationPattern.Parse("#{64}#{64}#{64}#{63}", "luhn").MaximumLength.ShouldEqual(256);
        GenerationPattern.Parse("#{64}#{64}#{64}#{62}", "mod97").MaximumLength.ShouldEqual(256);
    }
}
