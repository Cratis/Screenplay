// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_a_route_feeding_example_value : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();

    void Because() => _result = Bind(
        """
        concept LedgerKey : Int
        example Input : C
          id = "one"
          key = 9007199254740991
        eventsource Account
          identifier String
          stream Ledger
            streamId LedgerKey
        module M
          feature F
            slice StateChange S
              command C
                id String identifier
                key LedgerKey
                stream Account.Ledger
                  streamId = key
                produces event E
                  count Int = 42
              specification Routed
                when Input
                then E
                  stream Account.Ledger
                    streamId = 9007199254740991
                  count = 42
        """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v8() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V8);
    [Fact] void should_keep_semantic_v8() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_keep_the_route_input_exact() => Slice.Specifications.Single().When!.Values.Single(value => value.TargetProperty == Slice.Commands.Single().Properties.Single(property => property.Name == "key").Id).Value.ShouldEqual(SemanticValue.Number(9007199254740991m));
    [Fact] void should_preserve_the_model_on_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
}
