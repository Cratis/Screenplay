// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_uuid_key_casing : given.a_protected_command
{
    const string ReadId = "a1111111-1111-4111-8111-111111111111";
    const string WriteId = "b2222222-2222-4222-8222-222222222222";

    void Establish()
    {
        Compile(Source.Replace("id String", "id Uuid", StringComparison.Ordinal).Replace("lookup String", "lookup Uuid", StringComparison.Ordinal));
        _world = World(UuidFact("Recorded", ReadId, "original"));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    void should_match_dependencies_ordinally(bool differentCase, bool matches)
    {
        var decision = _evaluator.Decide(_plan, _world, Request(lookup: ReadId, id: WriteId));
        decision.Dependencies.Matches(UuidFact("Removed", differentCase ? ReadId.ToUpperInvariant() : ReadId)).ShouldEqual(matches);
    }

    [Fact]
    void should_conflict_on_a_canonical_removal()
    {
        var decision = _evaluator.Decide(_plan, _world, Request(lookup: ReadId, id: WriteId));
        var latest = Append(_world, UuidFact("Removed", ReadId));
        ((SemanticConflict)decision.Commit(latest)).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    }

    [Fact]
    void should_refuse_a_noncanonical_key_before_it_reaches_the_world()
    {
        var result = (SemanticRejected)_evaluator.ExtendWorld(_plan, _world, [UuidFact("Removed", ReadId.ToUpperInvariant())]);
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.ShouldBeSame(_world);
    }

    SemanticFact UuidFact(string name, string key, string? value = null) => Fact(name, key, value) with
    {
        Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), SemanticValue.Text(key)))
    };
}
