// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_protected_enumeration_keys : Specification
{
    [Fact]
    void should_apply_the_shared_enumeration_key_refusal_to_programmatic_models()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV25();
        var conceptId = SemanticId.Create(SemanticAddress.ForConcept(ApplicationIdentity.Create("Projects"), "Key"));
        var type = SemanticTypeReference.ForConcept(conceptId);
        var application = model.Application with
        {
            Concepts = [new(conceptId, "Key", SemanticPrimitiveType.Text, ["one", "two"], [])],
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with
                        {
                            Properties = [.. command.Properties.Select(property => property.IsIdentifier ? property with { Type = type } : property)],
                            Destination = command.Destination! with { Type = type }
                        })],
                        ReadModels = [.. slice.ReadModels.Select(view => view with
                        {
                            Properties = [.. view.Properties.Select(property => property.IsIdentifier ? property with { Type = type } : property)]
                        })]
                    })]
                })]
            })]
        };
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V25, SemanticVersion.V25, application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error!.Message.ShouldContain("unprotectable: KeyType.");
    }
}
