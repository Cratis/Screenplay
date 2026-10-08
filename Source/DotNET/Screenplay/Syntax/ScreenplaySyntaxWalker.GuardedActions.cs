// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of guarded screen actions and their ordered alternatives.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a guarded action and its choices, fallback and navigation.
    /// </summary>
    /// <param name="syntax">The action to visit.</param>
    public virtual void VisitScreenGuardedAction(ScreenGuardedActionSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var alternative in syntax.Alternatives) VisitScreenActionAlternative(alternative);
        if (syntax.Otherwise is not null) VisitScreenActionOtherwise(syntax.Otherwise);
        if (syntax.Navigate is not null) VisitScreenNavigate(syntax.Navigate);
    }

    /// <summary>
    /// Visits an alternative, its structured condition and input bindings.
    /// </summary>
    /// <param name="syntax">The alternative to visit.</param>
    public virtual void VisitScreenActionAlternative(ScreenActionAlternativeSyntax syntax)
    {
        VisitNode(syntax);
        VisitCondition(syntax.Condition);
        foreach (var argument in syntax.Arguments) VisitInteractionArgument(argument);
    }

    /// <summary>
    /// Visits a fallback and its input bindings.
    /// </summary>
    /// <param name="syntax">The fallback to visit.</param>
    public virtual void VisitScreenActionOtherwise(ScreenActionOtherwiseSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var argument in syntax.Arguments) VisitInteractionArgument(argument);
    }
}
