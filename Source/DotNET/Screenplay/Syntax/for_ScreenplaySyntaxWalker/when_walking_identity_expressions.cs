// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_identity_expressions : Specification
{
    IdentityWalker _walker;
    ExpressionSyntax _expression;

    void Establish()
    {
        _walker = new();
        _expression = new IdentityExpressionSyntax("userName", new(1, 1));
    }

    void Because() => _walker.VisitExpression(_expression);

    [Fact] void should_dispatch_to_the_identity_visit() => _walker.Identity.ShouldEqual(_expression);
    [Fact] void should_visit_the_node_by_default() => _walker.Node.ShouldEqual(_expression);

    sealed class IdentityWalker : ScreenplaySyntaxWalker
    {
        public IdentityExpressionSyntax? Identity { get; private set; }
        public SyntaxNode? Node { get; private set; }

        public override void VisitIdentityExpression(IdentityExpressionSyntax syntax)
        {
            Identity = syntax;
            base.VisitIdentityExpression(syntax);
        }

        public override void VisitNode(SyntaxNode node) => Node = node;
    }
}
