// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_double_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("quantity", new LiteralExpressionSyntax(3.5d, SourceLocation.Start)));

    [Fact] void should_reject_a_typed_number_that_cannot_round_trip_through_text() => Result.Accepted.ShouldBeFalse();
    [Fact] void should_not_expose_a_partial_candidate() => Result.Workspace.ShouldBeNull();
}
