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
        var module = application.Modules[0];
        var feature = module.Features.Single(value => value.Id == Id(10560));
        var slice = feature.Slices[0];
        var contract = slice.Events[0] with { StoredName = "OriginalEvent" };
        slice = slice with { Events = slice.Events.SetItem(0, contract) };
        feature = feature with { Slices = feature.Slices.SetItem(0, slice) };
        module = module with { Features = [.. module.Features.Select(value => value.Id == feature.Id ? feature : value)] };
        application = application with { Modules = application.Modules.SetItem(0, module) };
        return ExecutableSemanticModel.Create(LanguageVersion.V16, SemanticVersion.V16, application);
    }
}
#endif
