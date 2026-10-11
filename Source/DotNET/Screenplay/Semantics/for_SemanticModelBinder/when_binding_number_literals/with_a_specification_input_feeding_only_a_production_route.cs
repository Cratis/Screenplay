// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_a_specification_input_feeding_only_a_production_route : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
    SemanticCommand Command => Slice.Commands.Single();

    void Because() => _result = Bind(
        """
        concept LedgerKey : Int
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
                produces event E
                  stream Account.Ledger
                    streamId = key
                  count Int = 42
              specification Routed
                when C
                  id = "one"
                  key = 9007199254740991
                then E
                  stream Account.Ledger
                    streamId = 9007199254740991
                  count = 42
        """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v10() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
    [Fact] void should_keep_semantic_v10() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    [Fact] void should_have_no_command_route() => Command.Route.ShouldBeNull();
    [Fact] void should_keep_the_route_input_exact() => Slice.Specifications.Single().When!.Values.Single(value => value.TargetProperty == Command.Properties.Single(property => property.Name == "key").Id).Value.ShouldEqual(SemanticValue.Number(9007199254740991m));
    [Fact] void should_not_activate_exact_number_literals() => SemanticModelValidator.UsesExactNumberLiterals(_result.Value!.Model.Application, SemanticVersion.V10).ShouldBeFalse();
    [Fact] void should_accept_and_preserve_the_model_on_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
    [Fact] void should_execute_with_the_unchanged_route_format() => ((SemanticAccepted)new SemanticSpecificationRunner().Run(_result.Value!, Slice.Specifications.Single().Id).Execution).Facts.Single().Route!.StreamId.ShouldEqual("9007199254740991");
}
