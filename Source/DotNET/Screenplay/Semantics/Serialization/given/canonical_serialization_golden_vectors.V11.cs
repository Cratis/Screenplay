// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV11Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v11.json");

    public static ExecutableSemanticModel CreateSemanticModelV11()
    {
        var application = CreateSemanticModelV10().Application;
        var whole = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber);
        var fraction = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber);
        var exactEvent = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(10100), "ExactNumbersRecorded",
            [new(Id(10101), "largest", whole, false), new(Id(10102), "smallest", whole, false), new(Id(10103), "whole", whole, false), new(Id(10104), "fraction", fraction, false)]);
        decimal[] values = [9007199254740991m, -9007199254740991m, 1234567890123456m, 0.10000000000000002m];
        var exactCommand = new SemanticCommand(Id(10110), "RecordExactNumbers", [], [],
            [new(exactEvent.Id, null, null, [.. exactEvent.Properties.Select((property, index) => new SemanticPropertyMapping(property.Id, SemanticExpression.FromValue(SemanticValue.Number(values[index]))))])]);
        var exactSlice = new SemanticSlice(Id(10120), "ExactNumbers", SemanticSliceKind.StateChange, [exactEvent], [exactCommand], [], [], [], []);
        var module = application.Modules[0];
        var feature = module.Features.Single(feature => feature.Id == Id(9300));
        var replacement = feature with { Slices = feature.Slices.Add(exactSlice) };
        application = application with { Modules = application.Modules.SetItem(0, module with { Features = [.. module.Features.Select(value => value.Id == replacement.Id ? replacement : value)] }) };

        return ExecutableSemanticModel.Create(LanguageVersion.V11, SemanticVersion.V11, application);
    }
}
#endif
