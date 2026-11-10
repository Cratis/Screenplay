// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>Visits the explicit agent allowlist.</summary>
    /// <param name="syntax">The allowlist to visit.</param>
    public virtual void VisitAgents(AgentsSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Tokens is not null) VisitAgentTokens(syntax.Tokens);
        foreach (var operation in syntax.Operations) VisitAgentOperation(operation);
    }

    /// <summary>Visits an exposed operation reference.</summary>
    /// <param name="syntax">The reference to visit.</param>
    public virtual void VisitAgentOperation(AgentOperationSyntax syntax) => VisitNode(syntax);

    /// <summary>Visits the token declaration.</summary>
    /// <param name="syntax">The declaration to visit.</param>
    public virtual void VisitAgentTokens(AgentTokensSyntax syntax) => VisitNode(syntax);

    void VisitAgentDeclarations(ApplicationSyntax syntax)
    {
        if (syntax.Agents is not null) VisitAgents(syntax.Agents);
    }
}
