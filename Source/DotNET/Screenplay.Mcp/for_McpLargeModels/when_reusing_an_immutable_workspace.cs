// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_reusing_an_immutable_workspace : for_McpConnection.given.a_connection
{
    McpWorkspaceAnalysis _first = null!;
    McpWorkspaceAnalysis _second = null!;

    void Because()
    {
        var workspace = Workspace();
        _first = McpWorkspaceAnalysis.For(workspace);
        _second = McpWorkspaceAnalysis.For(workspace);
    }

    [Fact] void should_reuse_analysis_for_the_same_immutable_workspace() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_reuse_source_compilation() => ReferenceEquals(_first.Source.Compilation, _second.Source.Compilation).ShouldBeTrue();
    [Fact] void should_reuse_the_occurrence_index() => ReferenceEquals(_first.Syntax, _second.Syntax).ShouldBeTrue();
    [Fact] void should_serialize_the_canonical_export_once() => ReferenceEquals(_first.ExportBytes, _second.ExportBytes).ShouldBeTrue();
}
