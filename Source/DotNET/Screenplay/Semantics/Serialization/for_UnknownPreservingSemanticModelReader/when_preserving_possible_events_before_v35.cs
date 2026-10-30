// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_preserving_possible_events_before_v35 : Specification
{
    byte[] _bytes;
    UnknownPreservingSemanticDocument _document;

    void Because()
    {
        var model = Serialization.given.canonical_serialization_golden_vectors.CreateSemanticModelV34();
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command.Produces.IsEmpty ? command : command with
                        {
                            Handler = new(ImplementationRequirementIdentity.Create(command.Id, SemanticImplementationRole.CommandHandler, null), 1)
                        })]
                    })]
                })]
            })]
        };
        var baseline = ExecutableSemanticModel.Create(LanguageVersion.V34, SemanticVersion.V34, application);
        var root = JsonNode.Parse(SemanticModelSerializer.Serialize(baseline))!.AsObject();
        root["languageVersion"] = $"34.{uint.MaxValue}";
        root["semanticVersion"] = $"34.{uint.MaxValue}";
        documents.Find(root, "handler")["possibleEvents"] = new JsonArray();
        _bytes = documents.Seal(root);
        _document = UnknownPreservingSemanticModelReader.Read(_bytes, documents.Range(34));
    }

    [Fact] void should_preserve_the_future_member() => _document.Unknowns.Any(member => member.Name == "possibleEvents").ShouldBeTrue();
    [Fact] void should_reemit_the_wire_bytes() => _document.CopyBytes().ShouldEqual(_bytes);
}
#endif
