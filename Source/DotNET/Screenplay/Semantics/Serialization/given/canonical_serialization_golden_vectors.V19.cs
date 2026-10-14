// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV19Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v19.json");

    public static ExecutableSemanticModel CreateSemanticModelV19()
    {
        var application = CreateSemanticModelV17().Application;
        var identity = ApplicationIdentity.Create("Canonical Golden Application");
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var observed = Event(identity, Id(10710), "ClaimObserved", []);
        var claimed = Event(identity, Id(10711), "ClaimAccepted", []);
        var refused = Event(identity, Id(10712), "ClaimRefused", [new(Id(10713), "reason", text, false), new(Id(10714), "constraint", text, false), new(Id(10715), "message", text, false)]);
        var command = new SemanticCommand(Id(10720), "ClaimOnce", [new(Id(10721), "id", text, true)], [], [new(claimed.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(10721)), [])])
        {
            Destination = new(text, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(10721)))
        };
        var branch = new SemanticProducedEvent(
            refused.Id,
            null,
            null,
        [
            new(Id(10713), new SemanticRefusalExpression("reason", text)),
            new(Id(10714), new SemanticRefusalExpression("constraint", text)),
            new(Id(10715), new SemanticRefusalExpression("message", text))
        ]);
        var invocation = new SemanticInvocation(command.Id, [new(Id(10721), SemanticExpression.FromValue(SemanticValue.Text("claim")))])
        {
            OnRefused = [new("constraint", "ClaimIsUnique", [branch]), new("validation", null, []), new("any", null, []), new("authorization", null, [])]
        };
        var reaction = new SemanticReaction(Id(10730), "ClaimObserver", [new(SemanticReactionTriggerKind.Event) { Source = observed.Id, Invokes = [invocation] }]);
        var constraint = new SemanticConstraint("ClaimIsUnique", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(claimed.Id, [])], [], false, "Already claimed");
        var slice = new SemanticSlice(Id(10740), "Refusal", SemanticSliceKind.Automation, [observed, claimed, refused], [command], [], [], [], []) { Reactions = [reaction], Constraints = [constraint] };
        var module = application.Modules[0];
        application = application with { Modules = application.Modules.SetItem(0, module with { Features = module.Features.Add(new(Id(10750), "InvocationRefusals", [], [slice])) }) };

        return ExecutableSemanticModel.Create(LanguageVersion.V19, SemanticVersion.V19, application);
    }
}
#endif
