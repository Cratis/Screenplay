// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_double_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("quantity", new LiteralExpressionSyntax(3.5d, SourceLocation.Start)));

    [Fact] void should_accept_an_existing_double_edit_without_changing_its_numeric_value() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_literal() => Candidate().ShouldEqual(Bytes(OrderSource.Replace("quantity =   2", "quantity =   3.5", StringComparison.Ordinal)));
}
