// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_whole_property_mapping;

public class with_a_different_source_kind : given.a_document_with_literal_values
{
    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping && string.Equals(mapping.Property, "channel", StringComparison.Ordinal) && entry.Handle.Path.Contains("/produces/", StringComparison.Ordinal));
        Result = Propose(new ReplaceWorkspaceNode(entry.Handle, entry.Node, new PropertyMappingSyntax("channel", new LiteralExpressionSyntax("web", SourceLocation.Start), SourceLocation.Start)));
    }

    [Fact] void should_accept_the_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_mapping_source() => Candidate().ShouldEqual(Bytes(OrderSource.Replace("channel = channel", "channel = \"web\"", StringComparison.Ordinal)));
    [Fact] void should_not_disclose_normalization() => Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeFalse();
}
