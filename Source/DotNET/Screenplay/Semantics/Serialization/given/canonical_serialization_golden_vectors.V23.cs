// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV23Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v23.json");

    public static ExecutableSemanticModel CreateSemanticModelV23()
    {
        var application = CreateSemanticModelV22().Application;
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var identity = ApplicationIdentity.Create("Canonical Golden Application");
        var @event = Event(identity, Id(123001), "PayloadChosen", [new(Id(123002), "value", text, false)]);
        var command = new SemanticCommand(Id(123003), "ChoosePayload", [], [], [new(@event.Id, null, null, [])])
        {
            Handler = new(ImplementationRequirementIdentity.Create(Id(123003), SemanticImplementationRole.CommandHandler, null), 1)
        };
        var slice = new SemanticSlice(Id(123004), "Described", SemanticSliceKind.StateChange, [@event], [command], [], [], [], []);
        var module = application.Modules[0];
        application = application with { Modules = application.Modules.SetItem(0, module with { Features = module.Features.Add(new(Id(123005), "DescribedHandlers", [], [slice])) }) };

        return ExecutableSemanticModel.Create(LanguageVersion.V23, SemanticVersion.V23, application, 2026);
    }
}
#endif
