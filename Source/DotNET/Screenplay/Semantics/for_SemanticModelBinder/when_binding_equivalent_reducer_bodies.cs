// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_equivalent_reducer_bodies : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balances
              event Deposited
                amount Decimal
              readmodel Balance
                id Uuid
              query ById => Balance?
                by id Uuid
              reducer Fold => Balance
                on Deposited
                  file Reducers/Deposited.cs
        """;

    CompilationResult<SemanticCompilation> _file;
    CompilationResult<SemanticCompilation> _inline;

    void Because()
    {
        _file = Bind(Source);
        _inline = Bind(Source.Replace("file Reducers/Deposited.cs", "```csharp\n          return new Balance();\n          ```", StringComparison.Ordinal));
    }

    [Fact] void should_bind_both_forms() => (_file.Success && _inline.Success).ShouldBeTrue();
    [Fact] void should_have_identical_portable_contract_bytes() =>
        SemanticModelSerializer.Serialize(_file.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_inline.Value!.Model)).ShouldBeTrue();
    [Fact] void should_share_the_requirement_identity() => _file.ImplementationRequirements.Single().RequirementId.ShouldEqual(_inline.ImplementationRequirements.Single().RequirementId);
}
