// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_null_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("note", new LiteralExpressionSyntax("none", SourceLocation.Start)));

    [Fact] void should_accept_the_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_literal() => Candidate().ShouldEqual(Bytes(OrderSource.Replace("note = null", "note = \"none\"", StringComparison.Ordinal)));
    [Fact] void should_not_disclose_normalization() => Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeFalse();
}
