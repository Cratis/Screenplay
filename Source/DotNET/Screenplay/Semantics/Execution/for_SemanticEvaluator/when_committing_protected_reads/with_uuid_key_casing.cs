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
    [InlineData(false)]
    [InlineData(true)]
    void should_match_dependencies_and_projection_keys_ordinally(bool differentCase)
    {
        var request = Request(lookup: ReadId, id: WriteId);
        var decision = _evaluator.Decide(_plan, _world, request);
        var removal = UuidFact("Removed", differentCase ? ReadId.ToUpperInvariant() : ReadId);

        // The internal fact seam permits a casing probe without changing canonical UUID request validation.
        var latest = Append(_world, removal);
        var view = _plan.ReadModels.Values.Single(view => view.Name == "View");
        latest.ReadModels.Any(instance => instance.ReadModel == view.Id).ShouldEqual(differentCase);
        decision.Dependencies.Matches(removal).ShouldEqual(!differentCase);
        var result = decision.Commit(latest);
        result.Kind.ShouldEqual(differentCase ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Conflict);
        if (result is SemanticAccepted accepted)
        {
            accepted.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
        }
    }

    SemanticFact UuidFact(string name, string key, string? value = null) => Fact(name, key, value) with
    {
        Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), SemanticValue.Text(key)))
    };
}
