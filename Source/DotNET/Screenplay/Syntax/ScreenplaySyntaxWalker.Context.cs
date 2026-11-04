// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits the application context and its named time-zone members.
    /// </summary>
    /// <param name="syntax">The context to visit.</param>
    public virtual void VisitApplicationContext(ApplicationContextSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var member in syntax.Members) VisitContextMember(member);
    }

    /// <summary>
    /// Visits a named time-zone context member.
    /// </summary>
    /// <param name="syntax">The member to visit.</param>
    public virtual void VisitContextMember(ContextMemberSyntax syntax) => VisitNode(syntax);
}
