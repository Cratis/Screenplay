// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV10Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v10.json");

    public static ExecutableSemanticModel CreateReactionIdentityModelV10()
    {
        var application = CreateSemanticModelV9().Application;
        var feature = new SemanticFeature(
            Id(9800),
            "ReactionIdentities",
            [],
            [
                new(Id(9810), "SystemWork", SemanticSliceKind.Automation, [], [], [], [], [], [])
                {
                    Reactions =
                    [
                        new(Id(9820), "Privileged", [new(SemanticReactionTriggerKind.Startup)])
                        {
                            RunsAs = new(SemanticReactionIdentityKind.System, ["Auditor", "ClaimsAutomation"])
                        },
                        new(Id(9830), "Authenticated", [new(SemanticReactionTriggerKind.Shutdown)])
                        {
                            RunsAs = new(SemanticReactionIdentityKind.System, [])
                        }
                    ]
                }
            ]);
        application = application with { Modules = [.. application.Modules.Select((module, index) => index == 0 ? module with { Features = module.Features.Add(feature) } : module)] };

        return ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
    }
}
#endif
