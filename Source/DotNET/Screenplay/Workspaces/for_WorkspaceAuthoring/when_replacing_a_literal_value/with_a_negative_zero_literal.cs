// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_negative_zero_literal : given.a_document_with_literal_values
{
    void Because() => Result = Propose(ReplaceSource("quantity", new LiteralExpressionSyntax(-0d, SourceLocation.Start)));

    [Fact] void should_keep_mains_printed_zero() =>
        ScreenplaySyntaxText.Expression(new LiteralExpressionSyntax(-0d, SourceLocation.Start)).ShouldEqual("0");

    [Fact] void should_distinguish_signed_zero_on_authoring_round_trips() =>
        SyntaxJson.EquivalentForAuthoring(
            new LiteralExpressionSyntax(-0d, SourceLocation.Start),
            new LiteralExpressionSyntax(0d, SourceLocation.Start)).ShouldBeFalse();

    [Fact] void should_reject_the_edit_instead_of_silently_discarding_the_sign() => Result.Accepted.ShouldBeFalse();
}
