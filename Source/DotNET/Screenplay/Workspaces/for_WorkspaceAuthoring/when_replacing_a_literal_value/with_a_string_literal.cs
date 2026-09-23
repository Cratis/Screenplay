// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_string_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("remark", new LiteralExpressionSyntax("sturdy", SourceLocation.Start)));

    [Fact] void should_accept_the_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_literal() => Candidate().ShouldEqual(Bytes(OrderSource.Replace("remark = \"fragile\"  //", "remark = \"sturdy\"  //", StringComparison.Ordinal)));
    [Fact] void should_keep_the_bom() => Candidate().AsSpan(0, 3).SequenceEqual(Encoding.UTF8.Preamble).ShouldBeTrue();
    [Fact] void should_keep_every_comment() => Result.Workspace!.Documents.Single().Text.Split("//").Length.ShouldEqual(5);
    [Fact] void should_not_disclose_normalization() => Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeFalse();
}
