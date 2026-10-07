// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovery_encounters_an_identity_file_link : for_McpConnection.given.a_dynamic_connection
{
    McpWorkspaces _workspaces = null!;
    Exception? _failure;
    string _outsideState = null!;

    void Establish()
    {
        var models = Path.Combine(ModelPath, "Models");
        Directory.CreateDirectory(models);
        File.Move(Path.Combine(ModelPath, "application.play"), Path.Combine(models, "application.play"));
        _outsideState = Path.Combine(EmptyPath, McpState.FileName);
        File.WriteAllText(_outsideState, "outside state");
        var metadata = Path.Combine(ModelPath, ".screenplay");
        Directory.CreateDirectory(metadata);
        File.CreateSymbolicLink(Path.Combine(metadata, McpState.FileName), _outsideState);
        _workspaces = new() { ClientRoots = [new Uri(ModelPath).AbsoluteUri] };
    }

    void Because() => _failure = Catch.Exception(() => _workspaces.Open(McpJson.Empty));

    [Fact] void should_refuse_the_link_instead_of_reading_outside_state() => _failure.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_explain_the_metadata_path_conflict() => _failure!.Message.Contains("MetadataPathConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_outside_state() => File.ReadAllText(_outsideState).ShouldEqual("outside state");
}
