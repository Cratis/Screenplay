// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_reestablishing_published_history : given.a_published_history
{
    SemanticAccepted _established;
    SemanticAccepted _extended;

    void Because()
    {
        var evaluator = new SemanticEvaluator();
        _established = (SemanticAccepted)evaluator.EstablishWorld(_plan, _live.World.Facts);
        var privateHistory = (SemanticAccepted)evaluator.EstablishWorld(_plan, [_live.Facts[0]]);
        _extended = (SemanticAccepted)evaluator.ExtendWorld(_plan, privateHistory.World, [_published]);
    }

    [Fact] void should_emit_the_private_and_public_facts_once() => _live.Facts.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_private_typed_source() => _published.Context.ShouldEqual(_live.Facts[0].Context);
    [Fact] void should_retain_the_public_source_type() => _published.Context!.EventSource.Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid));
    [Fact] void should_reestablish_without_republication() => AssertFacts(_live.World.Facts, _established.World.Facts);
    [Fact] void should_extend_without_republication() => AssertFacts(_live.World.Facts, _extended.World.Facts);
    [Fact] void should_return_only_the_supplied_extension() => AssertFacts([_published], _extended.Facts);
    [Fact] void should_reestablish_the_read_model() => Assert.Equal<SemanticPropertyValue>(_live.World.ReadModels.Single().Values, _established.World.ReadModels.Single().Values);
    [Fact] void should_extend_the_read_model() => Assert.Equal<SemanticPropertyValue>(_live.World.ReadModels.Single().Values, _extended.World.ReadModels.Single().Values);
    [Fact] void should_preserve_the_read_model_key() => _established.World.ReadModels.Single().Key.ShouldEqual(_live.World.ReadModels.Single().Key);

    [Fact]
    void should_continue_publishing_after_reestablishment()
    {
        var request = _request with
        {
            Values = [.. _request.Values.Select(value => value.Value is SemanticNumberValue ? value with { Value = SemanticValue.Number(12.5m) } : value)]
        };
        var continued = (SemanticAccepted)new SemanticEvaluator().Execute(_plan, _established.World, request);
        continued.Facts.Length.ShouldEqual(2);
        continued.World.Facts.Length.ShouldEqual(4);
        continued.Facts.Single(fact => fact.EventContract == _published.EventContract).Context.ShouldEqual(_published.Context);
    }

    static void AssertFacts(IEnumerable<SemanticFact> expected, IEnumerable<SemanticFact> actual)
    {
        var pairs = expected.Zip(actual).ToArray();
        pairs.Length.ShouldEqual(expected.Count());
        pairs.Length.ShouldEqual(actual.Count());
        foreach (var (left, right) in pairs)
        {
            right.EventContract.ShouldEqual(left.EventContract);
            right.Destination.ShouldEqual(left.Destination);
            right.Context.ShouldEqual(left.Context);
            right.Route.ShouldEqual(left.Route);
            right.ReactionOrigin.ShouldEqual(left.ReactionOrigin);
            right.Occurred.ShouldEqual(left.Occurred);
            Assert.Equal<SemanticPropertyValue>(left.Values, right.Values);
            Assert.Equal<string>(left.Tags, right.Tags);
        }
    }
}
