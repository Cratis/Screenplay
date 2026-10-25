// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

// The invoked command is the automation's own, so its specification runs it in full.
public class with_a_reaction_input_feeding_only_a_production_route : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    SemanticCommand Command => Automation.Commands.Single();
    SemanticSlice Automation => Slices.Single(slice => slice.Name == "A");
    SemanticSlice[] Slices => [.. _result.Value!.Model.Application.Modules.Single().Features.Single().Slices];

    void Because() => _result = Bind(
        """
        concept LedgerKey : Int
        eventsource Account
          identifier String
          stream Ledger
            streamId LedgerKey
        module M
          feature F
            slice Automation A
              command C
                id String identifier
                key LedgerKey
                produces event E
                  stream Account.Ledger
                    streamId = key
                  count Int = 42
              reaction R
                when Startup
                  invokes C
                    id = "one"
                    key = 9007199254740991
              specification Routed
                when trigger Startup
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
    [Fact] void should_keep_the_route_input_exact() => ((SemanticValueExpression)Automation.Reactions.Single().Triggers.Single().Invokes.Single().Mappings.Single(mapping => mapping.TargetProperty == Command.Properties.Single(property => property.Name == "key").Id).Source).Value.ShouldEqual(SemanticValue.Number(9007199254740991m));
    [Fact] void should_not_activate_exact_number_literals() => SemanticModelValidator.UsesExactNumberLiterals(_result.Value!.Model.Application, SemanticVersion.V10).ShouldBeFalse();
    [Fact] void should_accept_and_preserve_the_model_on_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
    [Fact] void should_execute_with_the_unchanged_route_format() => ((SemanticAccepted)new SemanticSpecificationRunner().Run(_result.Value!, Automation.Specifications.Single().Id).Execution).Facts.Single().Route!.StreamId.ShouldEqual("9007199254740991");
}
