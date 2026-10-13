// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    internal const string FreshnessMessage = "A generated event source already has facts; retry the command with a fresh generated value.";

    static bool SameSource(SemanticValue left, SemanticValue right, bool uuid) =>
        uuid && left is SemanticTextValue first && right is SemanticTextValue second && Guid.TryParse(first.Value, out var firstUuid) && Guid.TryParse(second.Value, out var secondUuid)
            ? firstUuid == secondUuid
            : SemanticValueRules.AreEqual(left, right);

    // An unrouted prior fact (null destination) belongs to no event source this invocation can prove it shares,
    // so it never makes a generated source stale; only routed facts on the same canonical source do.
    SemanticRejected? RejectReusedGeneratedSource()
    {
        if (!plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15)) return null;
        foreach (var property in command.Properties.Where(property => property.IsGenerated))
        {
            var generated = _commandValues[property.Id];
            var uuid = plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target).Primitive == SemanticPrimitiveType.Uuid;
            if (_facts.Any(fact => SameSource(fact.Destination, generated, uuid)) &&
                world.Facts.Any(fact => fact.Destination is not SemanticNullValue && SameSource(fact.Destination, generated, uuid)))
            {
                return new(world, SemanticRejectionCategory.Concurrency, null, FreshnessMessage);
            }
        }

        return null;
    }
}
