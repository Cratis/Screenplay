// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_numbered_enumeration_members : given.a_semantic_binder
{
    const string Source = "concept Status : Enum\n  draft\n  issued\nmodule Billing\n  feature Invoicing\n    slice StateChange Issue\n      command Issue\n        id Uuid identifier\n        status Status\n        produces when status == \"issued\"\n          Issued\n            for id\n            status = status\n      event Issued\n        status Status\n      specification Issuing\n        when Issue\n          id = \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n          status = \"issued\"\n        then Issued\n          for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n          status = \"issued\"";
    CompilationResult<SemanticCompilation> _positional;
    CompilationResult<SemanticCompilation> _pinned;

    void Because()
    {
        _positional = Bind(Source);
        _pinned = Bind(Source.Replace("  draft\n  issued", "  draft = 0\n  issued = 1", StringComparison.Ordinal));
    }

    [Fact] void should_bind() => _pinned.Success.ShouldBeTrue();
    [Fact] void should_keep_execution_contracts_unchanged() => SemanticModelSerializer.Serialize(_pinned.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_positional.Value!.Model)).ShouldBeTrue();
    [Fact] void should_compare_conditions_by_name() => ((SemanticComparison)Slice.Commands.Single().Produces.Single().When!).Right.Value.ShouldEqual(SemanticValue.Text("issued"));
    [Fact] void should_keep_command_specification_values_named() => Slice.Specifications.Single().When!.Values[1].Value.ShouldEqual(SemanticValue.Text("issued"));
    [Fact] void should_keep_expected_event_values_named() => Slice.Specifications.Single().ThenEvents.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("issued"));

    SemanticSlice Slice => _pinned.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
}
