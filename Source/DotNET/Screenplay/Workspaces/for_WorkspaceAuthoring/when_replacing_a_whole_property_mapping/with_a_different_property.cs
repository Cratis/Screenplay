// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_whole_property_mapping;

public class with_a_different_property : given.a_document_with_literal_values
{
    void Because()
    {
        var entry = Value("note");
        Result = Propose(new ReplaceWorkspaceNode(entry.Handle, entry.Node, new PropertyMappingSyntax("comment", new LiteralExpressionSyntax("fragile", SourceLocation.Start), SourceLocation.Start)));
    }

    [Fact] void should_accept_the_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_mapping() => Candidate().ShouldEqual(Bytes(OrderSource.Replace("note = null", "comment = \"fragile\"", StringComparison.Ordinal)));
    [Fact] void should_not_disclose_normalization() => Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeFalse();
}
