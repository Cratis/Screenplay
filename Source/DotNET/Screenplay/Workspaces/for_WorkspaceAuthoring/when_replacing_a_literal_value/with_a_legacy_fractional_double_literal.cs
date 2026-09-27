// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_legacy_fractional_double_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("quantity", new LiteralExpressionSyntax(0.1d, SourceLocation.Start)));

    [Fact] void should_accept_the_edit_with_unchanged_executable_value() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_print_a_readable_fraction_without_changing_the_executable_value() =>
        Candidate().ShouldEqual(Bytes(OrderSource.Replace("quantity =   2", "quantity =   0.1", StringComparison.Ordinal)));
}
