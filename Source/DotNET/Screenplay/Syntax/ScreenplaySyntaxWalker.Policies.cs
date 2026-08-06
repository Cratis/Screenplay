// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of policies and the conditions they require.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="PolicySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="PolicySyntax"/> to visit.</param>
    public virtual void VisitPolicy(PolicySyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Condition is not null)
        {
            VisitPolicyCondition(syntax.Condition);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits a <see cref="PolicyConditionSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="PolicyConditionSyntax"/> to visit.</param>
    /// <remarks>
    /// A condition kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitPolicyCondition(PolicyConditionSyntax syntax)
    {
        switch (syntax)
        {
            case AuthenticatedConditionSyntax authenticated:
                VisitAuthenticatedCondition(authenticated);
                break;
            case RoleConditionSyntax role:
                VisitRoleCondition(role);
                break;
            case ClaimConditionSyntax claim:
                VisitClaimCondition(claim);
                break;
            case LogicalPolicyConditionSyntax logical:
                VisitLogicalPolicyCondition(logical);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits an <see cref="AuthenticatedConditionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="AuthenticatedConditionSyntax"/> to visit.</param>
    public virtual void VisitAuthenticatedCondition(AuthenticatedConditionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="RoleConditionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="RoleConditionSyntax"/> to visit.</param>
    public virtual void VisitRoleCondition(RoleConditionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ClaimConditionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ClaimConditionSyntax"/> to visit.</param>
    public virtual void VisitClaimCondition(ClaimConditionSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Matches is not null)
        {
            VisitExpression(syntax.Matches);
        }
    }

    /// <summary>
    /// Visits a <see cref="LogicalPolicyConditionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="LogicalPolicyConditionSyntax"/> to visit.</param>
    public virtual void VisitLogicalPolicyCondition(LogicalPolicyConditionSyntax syntax)
    {
        VisitNode(syntax);
        VisitPolicyCondition(syntax.Left);
        VisitPolicyCondition(syntax.Right);
    }
}
