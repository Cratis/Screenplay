// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class and_the_same_text_appears_elsewhere : given.a_document_with_literal_values
{
    const string LastValue = "channel = \"web\"";

    void Because() => Result = Propose(
        ReplaceSource("channel", new LiteralExpressionSyntax("store", SourceLocation.Start)),
        ReplaceSource("channel", new LiteralExpressionSyntax("store", SourceLocation.Start), 1));

    [Fact] void should_accept_the_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_addressed_literals() => Candidate().ShouldEqual(Bytes(
        $"{OrderSource[..^LastValue.Length].Replace("channel = \"web\"  // web", "channel = \"store\"  // web", StringComparison.Ordinal)}channel = \"store\""));
    [Fact] void should_keep_the_same_text_in_comments() => Result.Workspace!.Documents.Single().Text.Contains("// Orders keep their channel: channel = \"web\"", StringComparison.Ordinal).ShouldBeTrue();
}
