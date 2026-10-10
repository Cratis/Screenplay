// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_refusing_invalid_v10_routes_and_filters : Specification
{
    [Fact]
    void should_refuse_v10_members_at_pre_v10_schema_versions()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV10().Application;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V9, SemanticVersion.V9, application));
        var root = JsonNode.Parse(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV10()))!;
        root["schemaVersion"] = 9;
        root["languageVersion"] = "9.0";
        root["semanticVersion"] = "9.0";
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact]
    void should_refuse_foreign_streams_in_production_routes_and_filters()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV10().Application;
        var module = application.Modules[0];
        var feature = module.Features.Single(feature => feature.Name == "EventRoutes");
        var slice = feature.Slices[0];
        var foreign = application.EventSources.Single(source => source.Name == "Fallback").Streams[0].Id;
        var command = slice.Commands[0];
        var invalidCommand = command with { Produces = [command.Produces[0] with { Route = command.Produces[0].Route! with { Stream = foreign } }] };
        var invalidRouteFeature = feature with { Slices = feature.Slices.SetItem(0, slice with { Commands = slice.Commands.SetItem(0, invalidCommand) }) };
        Assert.Throws<InvalidSemanticContract>(() => Create(invalidRouteFeature));
        var automation = feature.Slices[1];
        var invalidFilterFeature = feature with { Slices = feature.Slices.SetItem(1, automation with { Reactions = [automation.Reactions[0] with { From = new(automation.Reactions[0].From!.Source, foreign) }] }) };
        Assert.Throws<InvalidSemanticContract>(() => Create(invalidFilterFeature));

        ExecutableSemanticModel Create(SemanticFeature replacement) => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10,
            application with { Modules = application.Modules.SetItem(0, module with { Features = [.. module.Features.Select(value => value.Id == feature.Id ? replacement : value)] }) });
    }
}
