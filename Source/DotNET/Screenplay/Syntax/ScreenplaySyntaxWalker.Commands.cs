// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of commands - their properties, authorization, validation, concurrency and what they produce.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="CommandSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CommandSyntax"/> to visit.</param>
    public virtual void VisitCommand(CommandSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var property in syntax.Properties)
        {
            VisitProperty(property);
        }

        foreach (var reads in syntax.Reads ?? [])
        {
            VisitReads(reads);
        }

        if (syntax.Authorize is not null)
        {
            VisitAuthorize(syntax.Authorize);
        }

        if (syntax.Concurrency is not null)
        {
            VisitConcurrency(syntax.Concurrency);
        }

        foreach (var validation in syntax.Validations)
        {
            VisitValidate(validation);
        }

        foreach (var produces in syntax.Produces)
        {
            VisitProduces(produces);
        }

        if (syntax.Handler is not null)
        {
            VisitHandler(syntax.Handler);
        }
    }

    /// <summary>
    /// Visits a <see cref="ConcurrencySyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ConcurrencySyntax"/> to visit.</param>
    public virtual void VisitConcurrency(ConcurrencySyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ReadsSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ReadsSyntax"/> to visit.</param>
    public virtual void VisitReads(ReadsSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="AuthorizeSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="AuthorizeSyntax"/> to visit.</param>
    public virtual void VisitAuthorize(AuthorizeSyntax syntax)
    {
        VisitNode(syntax);

        VisitPolicyRequirement(syntax.Requirement);
    }

    /// <summary>
    /// Visits a <see cref="PolicyRequirementSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="PolicyRequirementSyntax"/> to visit.</param>
    public virtual void VisitPolicyRequirement(PolicyRequirementSyntax syntax)
    {
        switch (syntax)
        {
            case PolicyReferenceSyntax reference:
                VisitPolicyReference(reference);
                break;
            case LogicalPolicyRequirementSyntax logical:
                VisitLogicalPolicyRequirement(logical);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="LogicalPolicyRequirementSyntax"/> node and its operands.
    /// </summary>
    /// <param name="syntax">The <see cref="LogicalPolicyRequirementSyntax"/> to visit.</param>
    public virtual void VisitLogicalPolicyRequirement(LogicalPolicyRequirementSyntax syntax)
    {
        VisitNode(syntax);
        VisitPolicyRequirement(syntax.Left);
        VisitPolicyRequirement(syntax.Right);
    }

    /// <summary>
    /// Visits a <see cref="PolicyReferenceSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="PolicyReferenceSyntax"/> to visit.</param>
    public virtual void VisitPolicyReference(PolicyReferenceSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ValidateSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ValidateSyntax"/> to visit.</param>
    /// <remarks>
    /// A validate kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitValidate(ValidateSyntax syntax)
    {
        switch (syntax)
        {
            case DeclarativeValidateSyntax declarative:
                VisitDeclarativeValidate(declarative);
                break;
            case CodeValidateSyntax code:
                VisitCodeValidate(code);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="DeclarativeValidateSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="DeclarativeValidateSyntax"/> to visit.</param>
    public virtual void VisitDeclarativeValidate(DeclarativeValidateSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var rule in syntax.Rules)
        {
            VisitValidationRule(rule);
        }

        foreach (var requirement in syntax.Requirements ?? [])
        {
            VisitRequirement(requirement);
        }
    }

    /// <summary>
    /// Visits a <see cref="RequirementSyntax"/> node and its condition.
    /// </summary>
    /// <param name="syntax">The <see cref="RequirementSyntax"/> to visit.</param>
    public virtual void VisitRequirement(RequirementSyntax syntax)
    {
        VisitNode(syntax);
        VisitCondition(syntax.Condition);
    }

    /// <summary>
    /// Visits a <see cref="CodeValidateSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CodeValidateSyntax"/> to visit.</param>
    public virtual void VisitCodeValidate(CodeValidateSyntax syntax)
    {
        VisitNode(syntax);
        VisitCodeBlock(syntax.Code);
    }

    /// <summary>
    /// Visits a <see cref="ValidationRuleSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ValidationRuleSyntax"/> to visit.</param>
    public virtual void VisitValidationRule(ValidationRuleSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Value is not null)
        {
            VisitExpression(syntax.Value);
        }

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits a <see cref="ProducesSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ProducesSyntax"/> to visit.</param>
    public virtual void VisitProduces(ProducesSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.When is not null)
        {
            VisitCondition(syntax.When);
        }

        foreach (var mapping in syntax.Mappings)
        {
            VisitPropertyMapping(mapping);
        }

        foreach (var tag in syntax.Tags ?? [])
        {
            VisitTag(tag);
        }
    }

    /// <summary>
    /// Visits a <see cref="PropertyMappingSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="PropertyMappingSyntax"/> to visit.</param>
    public virtual void VisitPropertyMapping(PropertyMappingSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Source);
    }

    /// <summary>
    /// Visits a <see cref="HandlerSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="HandlerSyntax"/> to visit.</param>
    public virtual void VisitHandler(HandlerSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits a <see cref="ConditionSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ConditionSyntax"/> to visit.</param>
    /// <remarks>
    /// A condition kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitCondition(ConditionSyntax syntax)
    {
        switch (syntax)
        {
            case ComparisonConditionSyntax comparison:
                VisitComparisonCondition(comparison);
                break;
            case LogicalConditionSyntax logical:
                VisitLogicalCondition(logical);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="ComparisonConditionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ComparisonConditionSyntax"/> to visit.</param>
    public virtual void VisitComparisonCondition(ComparisonConditionSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Right);
    }

    /// <summary>
    /// Visits a <see cref="LogicalConditionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="LogicalConditionSyntax"/> to visit.</param>
    public virtual void VisitLogicalCondition(LogicalConditionSyntax syntax)
    {
        VisitNode(syntax);
        VisitCondition(syntax.Left);
        VisitCondition(syntax.Right);
    }
}
