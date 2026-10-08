// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_typed_examples : given.a_semantic_binder
{
    const string Declarations =
        """
        concept Token : Uuid
        module Billing
          feature Invoicing
            slice StateChange Recording
              command Record
                id Uuid identifier
                amount Int
                token Token generated
                produces Recorded
                  for id
                  amount = amount
              event Recorded
                amount Int
              readmodel Balance
                id Uuid
                amount Int
              query BalanceById => Balance?
                by id Uuid
        """;

    const string Examples =
        """
        example Input : Record
          id = "11111111-1111-1111-1111-111111111111"
          amount = 10
          generated token = "22222222-2222-2222-2222-222222222222"
        example Fact : Recorded
          amount = 10
          for "11111111-1111-1111-1111-111111111111"
        example View : Balance
          id = "11111111-1111-1111-1111-111111111111"
          amount = 10
        """;

    const string ReferencingSteps = "\n      specification Recording\n        given Fact\n        given readmodel View\n        when Input amount = 20\n        then Fact amount = 20\n        then readmodel View\n      specification Appending\n        when append Fact amount = 20\n        then readmodel View exactly";
    const string ExpandedSteps = "\n      specification Recording\n        given Recorded\n          amount = 10\n          for \"11111111-1111-1111-1111-111111111111\"\n        given readmodel Balance\n          id = \"11111111-1111-1111-1111-111111111111\"\n          amount = 10\n        when Record\n          id = \"11111111-1111-1111-1111-111111111111\"\n          amount = 20\n          generated token = \"22222222-2222-2222-2222-222222222222\"\n        then Recorded\n          amount = 20\n          for \"11111111-1111-1111-1111-111111111111\"\n        then readmodel Balance\n          id = \"11111111-1111-1111-1111-111111111111\"\n          amount = 10\n      specification Appending\n        when append Recorded\n          amount = 20\n          for \"11111111-1111-1111-1111-111111111111\"\n        then readmodel Balance exactly\n          id = \"11111111-1111-1111-1111-111111111111\"\n          amount = 10";

    CompilationResult<SemanticCompilation> _examples;
    CompilationResult<SemanticCompilation> _expanded;

    void Because()
    {
        _examples = Bind(Examples + "\n" + Declarations + ReferencingSteps);
        _expanded = Bind(Declarations + ExpandedSteps);
    }

    [Fact] void should_bind_examples() => Assert.True(_examples.Success, string.Join('\n', _examples.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_bind_the_hand_expanded_spelling() => Assert.True(_expanded.Success, string.Join('\n', _expanded.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_have_identical_esm_bytes() => SemanticModelSerializer.Serialize(_examples.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_expanded.Value!.Model)).ShouldBeTrue();
    [Fact] void should_have_identical_revisions() => _examples.Value!.Model.Revision.ShouldEqual(_expanded.Value!.Model.Revision);
    [Fact] void should_select_v7_for_generated_example_fixtures() => _examples.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);

    [Fact]
    void should_execute_example_command_event_and_read_model_fixtures()
    {
        var plan = SemanticExecutionPlan.Compile(_examples.Value!.Model).Plan!;
        var run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "Recording").Id);
        Assert.True(run.Passed, string.Join('\n', run.Failures));
    }

    [Fact]
    void should_select_v2_for_event_sources_without_generated_values()
    {
        var source = (Examples + "\n" + Declarations + ReferencingSteps)
            .Replace("  generated token = \"22222222-2222-2222-2222-222222222222\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("        token Token generated\n", string.Empty, StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    }
}
