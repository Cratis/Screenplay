// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

internal static class ImplementationInvariants
{
    internal static void Validate(SyntaxNode node)
    {
        // Legacy structural trees remain transportable. Authoring also requires exact print/parse fidelity.
        // The new wrapper must never silently pick a payload, including before that admission step.
        if (node is HandlerSyntax { Implementation: not null } handler)
        {
            ValidateHandler(handler);
        }

        if (node is ImplementationSyntax { Hints: null })
        {
            throw new InvalidSyntaxJson("Implementation hints must be a collection.");
        }

        if (node is ImplementationHintSyntax hintNode && string.IsNullOrWhiteSpace(hintNode.Text))
        {
            throw new InvalidSyntaxJson("An implementation hint must be nonblank.");
        }
    }

    internal static void ValidateAuthoring(ApplicationSyntax application) => new AuthoringWalker().VisitApplication(application);

    static void ValidateHandler(HandlerSyntax handler)
    {
        if (handler.File is not null && handler.Code is not null)
        {
            throw new InvalidSyntaxJson("A handler has at most one file or inline payload.");
        }

        if (handler.File is null && handler.Code is null && handler.Implementation is null)
        {
            throw new InvalidSyntaxJson("A bare handler requires a payload or implementation wrapper.");
        }
    }

    sealed class AuthoringWalker : ScreenplaySyntaxWalker
    {
        public override void VisitHandler(HandlerSyntax syntax)
        {
            ValidateHandler(syntax);
            base.VisitHandler(syntax);
        }

        public override void VisitNode(SyntaxNode node) => Validate(node);
    }
}
