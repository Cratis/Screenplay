// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractResolver;

public class when_a_reaction_uses_an_extensions_trigger : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    const string Model = """
        concept Money : String

        system Payments
          extension "cratis/stripe" version 2

        module M
          feature F
            slice Automation S
              event Settled
                paymentIntentId String
              reaction Settle
                when Payments.PaymentSucceeded
                  paymentIntentId
                  produces Settled
                    for paymentIntentId
                    paymentIntentId = paymentIntentId
        """;

    ExtensionFixture _fixture;

    void Establish() => _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);

    void Destroy() => _fixture.Dispose();

    CompilationResult<SemanticCompilation> BindLocked(string model, string contract)
    {
        using var fixture = ExtensionFixture.Locked("cratis/stripe", "2", contract);
        ExtensionContractResolver.Current = fixture.Resolver();
        return Bind(model);
    }

    [Fact] void should_parse() => new ScreenplayCompiler().Compile(Model).Diagnostics.Select(d => d.Code + " " + d.Message).ToArray().ShouldBeEmpty();
    [Fact] void should_resolve_the_qualified_trigger() => BindLocked(Model, ExtensionContractSample.Stripe).Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Code + " " + d.Message).ShouldBeEmpty();
    [Fact] void should_report_each_use_after_a_swap_to_an_extension_without_the_trigger() => BindLocked(Model, ExtensionContractSample.Stripe.Replace("trigger PaymentSucceeded", "trigger Other")).Diagnostics.Select(d => d.Code).ShouldContain(DiagnosticCodes.ExtensionIncompatibleUse);
}
