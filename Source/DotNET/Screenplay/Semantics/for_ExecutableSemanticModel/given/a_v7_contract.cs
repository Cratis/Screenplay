// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

public class a_v7_contract : Specification
{
    protected static readonly SemanticId ConceptId = Id(1);
    protected static readonly SemanticId GeneratedId = Id(2);
    protected static readonly SemanticId InputId = Id(3);
    protected static readonly SemanticId CommandId = Id(4);
    protected static readonly SemanticTypeReference IdentityType = SemanticTypeReference.ForConcept(ConceptId);
    protected static readonly SemanticTypeReference NoteType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true);
    protected static readonly SemanticValue GeneratedValue = SemanticValue.Text("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11");

    protected static SemanticCommand Command => new(CommandId, "Create", [new(GeneratedId, "Id", IdentityType, true) { IsGenerated = true }, new(InputId, "Note", NoteType, false)], [], [])
    {
        Response = new SemanticScalarCommandResponse(GeneratedId, IdentityType)
    };

    protected static SemanticSpecification Scenario => new(Id(5), "creates", [], [], new(CommandId, [new(InputId, SemanticValue.Null)]) { GeneratedValues = [new(GeneratedId, GeneratedValue)] }, [], [], [], [])
    {
        ThenReturns = new SemanticScalarSpecificationResponse(GeneratedValue)
    };

    protected static SemanticApplication Application(SemanticCommand? command = null, SemanticSpecification? scenario = null, ImmutableArray<SemanticValidationRule> rules = default) =>
        new(Id(6), "App", [new(ConceptId, "Identity", SemanticPrimitiveType.Uuid, [], rules.IsDefault ? [] : rules)], [], [new(Id(7), "Module", [new(Id(8), "Feature", [], [new(Id(9), "Slice", SemanticSliceKind.StateChange, [], [command ?? Command], [], [], [], [scenario ?? Scenario])])])]);

    protected static ExecutableSemanticModel Create(SemanticApplication application) => ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application);
    protected static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");
    protected static void Refuse(SemanticApplication application) => Catch.Exception(() => Create(application)).ShouldBeOfExactType<InvalidSemanticContract>();
}
