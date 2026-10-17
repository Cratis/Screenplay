// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_refused_keys : given.a_protected_command
{
    [Theory]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData("#")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    [InlineData(" padded ")]
    [InlineData("\tpadded")]
    void should_reject_the_key_as_a_missing_view_even_when_the_world_holds_it(string key)
    {
        var world = World(Fact("Recorded", key, "stored"));
        var result = (SemanticRejected)_evaluator.Execute(_plan, world, Request(lookup: key));
        result.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        result.Details.ShouldEqual($"View '{key}' does not exist.");
        result.World.ShouldBeSame(world);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    void should_reject_the_key_in_an_empty_world(string key)
    {
        var result = (SemanticRejected)_evaluator.Execute(_plan, SemanticWorld.Empty, Request(lookup: key));
        result.Details.ShouldEqual($"View '{key}' does not exist.");
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("a*")]
    [InlineData("**")]
    void should_still_read_keys_that_are_not_refused(string key)
    {
        var world = World(Fact("Recorded", key, "stored"));
        _evaluator.Execute(_plan, world, Request(lookup: key)).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact]
    void should_keep_unprotected_use_of_a_refused_key_unchanged()
    {
        var declaration = _plan.Commands.Values.Single(value => value.Name == "Record");
        var request = SemanticExecutionRequest.Create(
            declaration.Id,
            [new(declaration.Properties.Single(property => property.Name == "id").Id, SemanticValue.Text("*")),
             new(declaration.Properties.Single(property => property.Name == "value").Id, SemanticValue.Text("stored"))],
            []);
        _evaluator.Execute(_plan, SemanticWorld.Empty, request).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact]
    void should_refuse_a_composite_key_when_any_part_is_refused()
    {
        var part = SemanticId.Parse($"sem1:{new string('a', 64)}");
        RefusedReadKeys.IsRefused(SemanticValue.Composite([new(part, SemanticValue.Text("fine")), new(part, SemanticValue.Text("a#b"))])).ShouldBeTrue();
        RefusedReadKeys.IsRefused(SemanticValue.Composite([new(part, SemanticValue.Text("fine"))])).ShouldBeFalse();
    }
}
