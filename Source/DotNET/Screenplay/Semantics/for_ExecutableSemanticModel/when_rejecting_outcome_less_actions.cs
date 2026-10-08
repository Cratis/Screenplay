// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_rejecting_outcome_less_actions : a_valid_semantic_model
{
    Exception _programmatic;
    Exception _strictReader;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(item => item.Specifications.Length > 0);
        var success = slice.Specifications.Single(item => item.ThenEvents.Length > 0);
        _programmatic = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Specifications = [success with { ThenEvents = [], ThenReadModels = [], ThenQueries = [] }] })));
        var json = JsonNode.Parse(SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, _application)))!;
        foreach (var module in json["application"]!["modules"]!.AsArray())
        {
            foreach (var feature in module!["features"]!.AsArray())
            {
                foreach (var owner in feature!["slices"]!.AsArray())
                {
                    foreach (var specification in owner!["specifications"]!.AsArray().Where(item => item!["thenEvents"]!.AsArray().Count > 0))
                    {
                        specification!["thenEvents"] = new JsonArray();
                        specification["thenReadModels"] = new JsonArray();
                        specification["thenQueries"] = new JsonArray();
                    }
                }
            }
        }

        _strictReader = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [Fact] void should_reject_programmatic_action_without_outcomes() => _programmatic.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_preserve_the_original_contract_message() => _programmatic.Message.ShouldEqual("A success specification must contain at least one success outcome.");
    [Fact] void should_reject_the_document_before_revision_verification() => _strictReader.Message.ShouldEqual("A success specification must contain at least one success outcome.");
}
#endif
