// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractResolver;

public class when_binding_a_model_that_names_an_extension : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    const string Model = """
        concept Money : String

        system Payments
          extension "cratis/stripe" version 2

        module M
          feature F
            slice StateView S
              readmodel Row
                id String key
                value String
              projection Build => Row
                from Recorded
                  id = $eventSourceId
                  value = value
              event Recorded
                value String
        """;

    ExtensionFixture _fixture;

    void Establish() => _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);

    CompilationResult<SemanticCompilation> BindLocked(string source)
    {
        ExtensionContractResolver.Current = _fixture.Resolver();
        return Bind(source);
    }

    void Destroy() => _fixture.Dispose();

    [Fact] void should_bind_without_diagnostics() => BindLocked(Model).Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_bind_the_contracts_abilities_into_the_system() => BindLocked(Model).Value!.Model.Application.Systems.Single().Abilities.Select(ability => ability.Name).Single().ShouldEqual("CreatePaymentIntent");
    [Fact] void should_report_a_reference_missing_from_the_lock() => BindLocked(Model.Replace("\"cratis/stripe\" version 2", "\"cratis/other\"")).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.ExtensionNotLocked);
}
