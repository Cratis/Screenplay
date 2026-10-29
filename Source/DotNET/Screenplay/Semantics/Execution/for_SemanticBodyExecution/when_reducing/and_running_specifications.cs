// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_running_specifications : given.a_reducer
{
    const string Assertions = """
          then readmodel Balance
            accountId = "first"
          then query BalanceById
            arguments
              accountId = "first"
            result
              accountId = "first"
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_fold_an_appended_event_and_answer_the_final_query(bool scenario)
    {
        CompileSpecification("""
            specification Appending
              when append AmountDeposited
                for "first"
                accountId = "first"
            """ + "\n" + Assertions,
            scenario);
        Run().Passed.ShouldBeTrue();
        Contexts.Count.ShouldEqual(1);
    }

    [Fact]
    void should_fold_given_history_before_a_read_only_assertion()
    {
        CompileSpecification("""
            specification GivenHistory
              given AmountDeposited
                for "first"
                accountId = "first"
            """ + "\n" + Assertions);
        Run().Passed.ShouldBeTrue();
        Contexts.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_fold_a_command_after_a_given_read_model(bool scenario)
    {
        CompileSpecification("""
            specification Seeded
              given readmodel Balance
                accountId = "first"
              when Deposit
                accountId = "first"
              then AmountDeposited
                accountId = "first"
            """ + "\n" + Assertions,
            scenario,
            true);
        Run().Passed.ShouldBeTrue();
        SemanticValueRules.AreEqual(Contexts.Single().State!, State).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_fold_reaction_productions_in_both_batch_modes(bool atomic)
    {
        Reset();
        Configure((atomic ? "reaction batches atomic\n" : string.Empty) + Source + "\n" + """
                slice Automation FollowUp
                  command Start
                    accountId String identifier
                    produces Started
                      for accountId
                  event Started
                  reaction DepositAfterStart
                    when Started
                      produces AmountDeposited
                        accountId = $eventSourceId
                  specification Reacting
                    when append Started
                      for "first"
                    then AmountDeposited
                      accountId = "first"
            """ + "\n" + string.Join('\n', Assertions.Split('\n').Select(line => "      " + line)));
        Run().Passed.ShouldBeTrue();
        Contexts.Count.ShouldEqual(1);
        Contexts.Single().SequenceNumber.ShouldEqual(1UL);
    }

    void CompileSpecification(string specification, bool scenario = false, bool command = false)
    {
        Reset();
        var indented = string.Join('\n', specification.Split('\n').Select(line => "      " + line));
        var source = command ? Source.Replace("    slice StateView", indented + "\n    slice StateView", StringComparison.Ordinal) : Source + "\n" + indented;
        Configure((scenario ? "reaction batches atomic\n" : string.Empty) + source);
    }

    SemanticSpecificationRun Run() => new SemanticSpecificationRunner(Evaluator).Run(Plan, Plan.Specifications.Values.Single().Id);
}
