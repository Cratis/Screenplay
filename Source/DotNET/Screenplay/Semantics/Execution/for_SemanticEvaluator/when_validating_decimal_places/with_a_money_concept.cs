// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_validating_decimal_places;

public class with_a_money_concept : Specification
{
    const string Source =
        """
        concept Money : Decimal
          validate
            decimals <= 2 message "Money has at most two decimals"
        module Prices
          feature Pricing
            slice StateChange Setting
              event Priced
                amount Money
              command SetPrice
                id String identifier
                amount Money
                produces Priced
                  for id
                  amount = amount
        """;

    SemanticExecutionPlan _plan;
    SemanticCommand _command;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Prices"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("prices"), "prices", "Prices.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Prices", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(_ => _.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model, compilation.Value.Model.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Select(_ => _.Id).ToArray());
        plan.Success.ShouldBeTrue();
        _plan = plan.Plan!;
        _command = _plan.Commands.Values.Single();
    }

    SemanticExecutionResult Execute(decimal amount) =>
        new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(_command.Id, [new(_command.Properties.First(_ => _.Name == "id").Id, SemanticValue.Text("a")), new(_command.Properties.First(_ => _.Name == "amount").Id, SemanticValue.Number(amount))], []));

    [Theory]
    [InlineData("0")]
    [InlineData("19")]
    [InlineData("19.9")]
    [InlineData("19.99")]
    [InlineData("19.990")]
    void should_accept_up_to_two_decimals(string amount) => Execute(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).ShouldBeOfExactType<SemanticAccepted>();

    [Theory]
    [InlineData("19.999")]
    [InlineData("0.001")]
    [InlineData("-5.125")]
    void should_reject_more_than_two_decimals_with_the_authored_message(string amount)
    {
        var result = Execute(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
        ((SemanticRejected)result).Details.ShouldContain("two decimals");
    }
}
