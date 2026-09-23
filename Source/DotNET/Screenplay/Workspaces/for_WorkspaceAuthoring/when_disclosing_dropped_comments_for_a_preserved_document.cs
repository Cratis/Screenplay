// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_disclosing_dropped_comments_for_a_preserved_document : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("remark", new LiteralExpressionSyntax("sturdy", SourceLocation.Start)));

    [Fact] void should_change_the_document() => Result.WritePlan!.Entries.Single().Kind.ShouldEqual(WorkspaceWriteKind.Replaced);
    [Fact] void should_report_no_dropped_comments() => WorkspaceDroppedComments.In(Result.WritePlan!).ShouldBeEmpty();
}
