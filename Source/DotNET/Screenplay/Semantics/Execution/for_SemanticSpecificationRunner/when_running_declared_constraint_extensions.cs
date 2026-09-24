// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_declared_constraint_extensions : Specification
{
    const string Source =
        """
        module Payments
          feature Ledger
            slice StateChange RecordPayment
              command RecordPayment
                paymentId Uuid identifier
                produces PaymentRecorded
                  for paymentId
                  paymentId = paymentId
              event PaymentRecorded
                paymentId Uuid
              event PaymentImported
                paymentId Uuid
              event PaymentReversed
                paymentId Uuid
              constraint OnePaymentPerAttempt
                unique event PaymentRecorded
                unique event PaymentImported
                released by PaymentReversed
              specification ReversalFreesTheClaim
                given PaymentRecorded
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                given PaymentReversed
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when RecordPayment
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then PaymentRecorded
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              specification EitherEventClaimsTheName
                given PaymentImported
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when RecordPayment
                  paymentId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then error "Constraint 'OnePaymentPerAttempt' is violated: the event source already has the constrained event."
        """;

    SemanticSpecificationRun[] _runs;
    bool _compiled;

    void Because()
    {
        const string stableKey = "constraint-grammar-vector";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Payments"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(stableKey), stableKey, "Payments.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Payments", SemanticDocumentSet.Create([document], catalog));
        _compiled = compilation.Success;
        if (!_compiled)
        {
            return;
        }

        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _runs = [.. plan.Specifications.Values.Select(spec => runner.Run(plan, spec.Id))];
    }

    [Fact] void should_compile() => _compiled.ShouldBeTrue();
    [Fact] void should_pass_the_release_and_mutual_exclusion_specifications() => _runs.SelectMany(_ => _.Failures).ShouldBeEmpty();
}
