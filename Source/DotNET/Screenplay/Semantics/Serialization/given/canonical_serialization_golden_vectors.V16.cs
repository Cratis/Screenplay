// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV16Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v16.json");

    public static ExecutableSemanticModel CreateSemanticModelV16()
    {
        var application = CreateSemanticModelV15().Application;
        var observed = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(10610), "RecoveryObserved", []);
        var reaction = new SemanticReaction(Id(10620), "RecoveryObserver", [new(SemanticReactionTriggerKind.Event) { Source = observed.Id }]);
        var specification = Specification(Id(10630), "redelivery appends nothing") with
        {
            GivenEvents = [new(observed.Id, [])],
            WhenRedelivered = new(reaction.Id, 0),
            ThenNoEvents = true
        };
        var slice = new SemanticSlice(Id(10640), "Recovery", SemanticSliceKind.Automation, [observed], [], [], [], [], [specification]) { Reactions = [reaction] };
        var module = application.Modules[0];
        application = application with
        {
            Modules = application.Modules.SetItem(0, module with { Features = module.Features.Add(new(Id(10650), "RecoverySpecifications", [], [slice])) })
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V16, SemanticVersion.V16, application);
    }
}
#endif
