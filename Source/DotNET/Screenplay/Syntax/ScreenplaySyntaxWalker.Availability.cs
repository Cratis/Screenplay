// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits an <c>audience</c> declaration.
    /// </summary>
    /// <param name="syntax">The <see cref="AudienceSyntax"/> to visit.</param>
    public virtual void VisitAudience(AudienceSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <c>availability</c> declaration and the requirements it holds.
    /// </summary>
    /// <param name="syntax">The <see cref="AvailabilitySyntax"/> to visit.</param>
    public virtual void VisitAvailability(AvailabilitySyntax syntax)
    {
        VisitNode(syntax);
        foreach (var requirement in syntax.Requirements) VisitAvailabilityRequirement(requirement);
    }

    /// <summary>
    /// Visits an <c>available</c> gate and its requirement.
    /// </summary>
    /// <param name="syntax">The <see cref="AvailableSyntax"/> to visit.</param>
    public virtual void VisitAvailable(AvailableSyntax syntax)
    {
        VisitNode(syntax);
        VisitAvailabilityRequirement(syntax.Requirement);
    }

    /// <summary>
    /// Visits an availability requirement by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="AvailabilityRequirementSyntax"/> to visit.</param>
    /// <remarks>
    /// A requirement kind this walker does not know is visited as a node and not descended into.
    /// </remarks>
    public virtual void VisitAvailabilityRequirement(AvailabilityRequirementSyntax syntax)
    {
        switch (syntax)
        {
            case ConditionAvailabilityRequirementSyntax condition:
                VisitNode(condition);
                VisitCondition(condition.Condition);
                break;
            case LogicalAvailabilityRequirementSyntax logical:
                VisitNode(logical);
                VisitAvailabilityRequirement(logical.Left);
                VisitAvailabilityRequirement(logical.Right);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }
}
