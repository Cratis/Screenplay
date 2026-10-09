// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class when_exporting_a_routed_workspace : given.an_export
{
    byte[] _bytes;
    bool _aligned;
    ExecutableSemanticModel _model;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), for_McpAuthoringWorkflow.when_renaming_a_source_with_streams.RoutedSource);
        Initialize();
    }

    void Because()
    {
        (_, _bytes, _aligned) = ExportPages();
        _model = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_keep_page_offsets_aligned() => _aligned.ShouldBeTrue();
    [Fact] void should_round_trip_strict_canonical_bytes() => StrictRoundTrip(_bytes).ShouldBeTrue();
    [Fact] void should_export_source_and_owned_stream_declarations() => _model.Application.EventSources.Single().Streams.Length.ShouldEqual(2);
    [Fact] void should_export_the_command_route() => _model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Route.ShouldNotBeNull();
    [Fact] void should_export_the_fixture_route() => _model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenEvents.Single().Route.ShouldNotBeNull();
}
