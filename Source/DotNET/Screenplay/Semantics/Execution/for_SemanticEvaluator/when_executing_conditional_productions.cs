// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_conditional_productions : a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid identifier
                isProForma Bool
                amount Decimal
                validate
                  require amount > 0
                    message "Amount must be positive"
                produces when isProForma == true
                  InvoiceRegistered
                    for invoiceId
                    tag billing
                    invoiceId = invoiceId
              event InvoiceRegistered
                tag invoicing
                invoiceId Uuid
        """;

    SemanticExecutionResult _falseResult;
    SemanticExecutionResult _trueResult;
    SemanticExecutionResult _rejected;

    void Because()
    {
        var model = Bind(Source).Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var command = plan.Commands.Values.Single();
        var id = command.Properties.Single(_ => _.Name == "invoiceId");
        var amount = command.Properties.Single(_ => _.Name == "amount");
        var proForma = command.Properties.Single(_ => _.Name == "isProForma");
        var destination = SemanticValue.Text("00000000-0000-0000-0000-000000000123");
        var evaluator = new SemanticEvaluator();
        var values = new[] { new SemanticPropertyValue(id.Id, destination), new SemanticPropertyValue(amount.Id, SemanticValue.Number(10)), new SemanticPropertyValue(proForma.Id, SemanticValue.Boolean(false)) };
        _falseResult = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, [.. values], []));
        _trueResult = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, [.. values.Select(_ => _.TargetProperty == proForma.Id ? new SemanticPropertyValue(proForma.Id, SemanticValue.Boolean(true)) : _)], []));
        _rejected = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, [.. values.Select(_ => _.TargetProperty == amount.Id ? new SemanticPropertyValue(amount.Id, SemanticValue.Number(0)) : _)], []));
    }

    [Fact] void should_accept_without_facts_when_the_guard_is_false() => ((SemanticAccepted)_falseResult).Facts.ShouldBeEmpty();
    [Fact] void should_append_when_the_guard_is_true() => ((SemanticAccepted)_trueResult).Facts.Length.ShouldEqual(1);
    [Fact] void should_attach_ordered_tags() => ((SemanticAccepted)_trueResult).Facts.Single().Tags.ShouldContainOnly(["invoicing", "billing"]);
    [Fact] void should_reject_a_failed_requirement_with_its_message() => ((SemanticRejected)_rejected).Details.ShouldEqual("Amount must be positive");
}
