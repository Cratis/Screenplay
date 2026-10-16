// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world.given;

// Compares worlds by facts, read-model state and pending children; accepted results are not compared whole.
public static class world_comparison
{
    public static bool AreEqual(SemanticWorld first, SemanticWorld second) =>
        first.Facts.SequenceEqual(second.Facts) &&
        first.ReadModels.Length == second.ReadModels.Length &&
        first.ReadModels.Zip(second.ReadModels).All(pair => pair.First.ReadModel == pair.Second.ReadModel &&
            SemanticValueRules.AreEqual(pair.First.Key, pair.Second.Key) &&
            pair.First.Values.Length == pair.Second.Values.Length &&
            pair.First.Values.Zip(pair.Second.Values).All(value => value.First.TargetProperty == value.Second.TargetProperty &&
                SemanticValueRules.AreEqual(value.First.Value, value.Second.Value))) &&
        first.PendingChildren.Length == second.PendingChildren.Length &&
        first.PendingChildren.Zip(second.PendingChildren).All(pair => pair.First.Projection == pair.Second.Projection &&
            pair.First.Fact == pair.Second.Fact &&
            SemanticValueRules.AreEqual(pair.First.Identity, pair.Second.Identity) &&
            SemanticValueRules.AreEqual(pair.First.ParentIdentity, pair.Second.ParentIdentity));
}
