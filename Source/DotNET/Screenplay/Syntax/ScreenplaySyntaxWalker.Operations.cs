// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits an external system declaration.
    /// </summary>
    /// <param name="syntax">The system.</param>
    public virtual void VisitSystem(SystemSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var ability in syntax.Abilities) VisitAbility(ability);
    }

    /// <summary>
    /// Visits an ability, its inputs, returned values, outcomes and implementation.
    /// </summary>
    /// <param name="syntax">The ability.</param>
    public virtual void VisitAbility(AbilitySyntax syntax)
    {
        VisitNode(syntax);
        foreach (var input in syntax.Inputs) VisitProperty(input);
        foreach (var returned in syntax.Returns) VisitProperty(returned);
        foreach (var outcome in syntax.Outcomes) VisitAbilityOutcome(outcome);
        if (syntax.Implementation is not null) VisitImplementation(syntax.Implementation);
        if (syntax.File is not null) VisitFileReference(syntax.File);
        if (syntax.Code is not null) VisitCodeBlock(syntax.Code);
    }

    /// <summary>
    /// Visits an ability outcome.
    /// </summary>
    /// <param name="syntax">The outcome.</param>
    public virtual void VisitAbilityOutcome(AbilityOutcomeSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an operation and its inputs and phases.
    /// </summary>
    /// <param name="syntax">The operation.</param>
    public virtual void VisitOperation(OperationSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var input in syntax.Inputs) VisitProperty(input);
        if (syntax.Execute is not null) VisitOperationPhase(syntax.Execute);
        if (syntax.Compensate is not null) VisitOperationPhase(syntax.Compensate);
    }

    /// <summary>
    /// Visits a phase and its implementation intent and selected source.
    /// </summary>
    /// <param name="syntax">The phase.</param>
    public virtual void VisitOperationPhase(OperationPhaseSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Implementation is not null) VisitImplementation(syntax.Implementation);
        if (syntax.File is not null) VisitFileReference(syntax.File);
        if (syntax.Code is not null) VisitCodeBlock(syntax.Code);
    }

    /// <summary>
    /// Visits a failure fixture.
    /// </summary>
    /// <param name="syntax">The fixture.</param>
    public virtual void VisitSpecificationOperationFailure(SpecificationOperationFailureSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an operation assertion and its input assertions.
    /// </summary>
    /// <param name="syntax">The assertion.</param>
    public virtual void VisitSpecificationOperation(SpecificationOperationSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var value in syntax.Values) VisitPropertyMapping(value);
    }

    /// <summary>
    /// Visits a compensation assertion.
    /// </summary>
    /// <param name="syntax">The assertion.</param>
    public virtual void VisitSpecificationCompensated(SpecificationCompensatedSyntax syntax) => VisitNode(syntax);
}
