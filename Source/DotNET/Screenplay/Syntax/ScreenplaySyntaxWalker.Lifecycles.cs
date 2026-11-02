// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="StateMachineSyntax"/> node and its states.
    /// </summary>
    /// <param name="syntax">The <see cref="StateMachineSyntax"/> to visit.</param>
    public virtual void VisitStateMachine(StateMachineSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var state in syntax.States) VisitState(state);
        foreach (var specification in syntax.Specifications) VisitLifecycleSpecification(specification);
    }

    /// <summary>
    /// Visits a <see cref="StateSyntax"/> node and its entries and transitions.
    /// </summary>
    /// <param name="syntax">The <see cref="StateSyntax"/> to visit.</param>
    public virtual void VisitState(StateSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var entry in syntax.Entering) VisitStateEntry(entry);
        foreach (var transition in syntax.Transitions) VisitStateTransition(transition);
    }

    /// <summary>
    /// Visits a <see cref="StateEntrySyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="StateEntrySyntax"/> to visit.</param>
    public virtual void VisitStateEntry(StateEntrySyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="StateTransitionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="StateTransitionSyntax"/> to visit.</param>
    public virtual void VisitStateTransition(StateTransitionSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.When is not null) VisitCondition(syntax.When);
    }

    /// <summary>
    /// Visits a <see cref="WorkflowSyntax"/> node and its nodes.
    /// </summary>
    /// <param name="syntax">The <see cref="WorkflowSyntax"/> to visit.</param>
    public virtual void VisitWorkflow(WorkflowSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var node in syntax.Nodes) VisitWorkflowNode(node);
        foreach (var specification in syntax.Specifications) VisitLifecycleSpecification(specification);
    }

    /// <summary>
    /// Visits a <see cref="WorkflowNodeSyntax"/> node and its edges.
    /// </summary>
    /// <param name="syntax">The <see cref="WorkflowNodeSyntax"/> to visit.</param>
    public virtual void VisitWorkflowNode(WorkflowNodeSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Schedule is not null) VisitNode(syntax.Schedule);
        if (syntax.Timeout is not null) VisitNode(syntax.Timeout);
        if (syntax.RetryAfter is not null) VisitNode(syntax.RetryAfter);
        if (syntax.Condition is not null) VisitCondition(syntax.Condition);
        if (syntax.Duration is not null) VisitNode(syntax.Duration);
        if (syntax.ExpiresAfter is not null) VisitNode(syntax.ExpiresAfter);
        foreach (var edge in syntax.Edges) VisitWorkflowEdge(edge);
    }

    /// <summary>
    /// Visits a <see cref="WorkflowEdgeSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="WorkflowEdgeSyntax"/> to visit.</param>
    public virtual void VisitWorkflowEdge(WorkflowEdgeSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="LifecycleSpecificationSyntax"/> node and its steps.
    /// </summary>
    /// <param name="syntax">The <see cref="LifecycleSpecificationSyntax"/> to visit.</param>
    public virtual void VisitLifecycleSpecification(LifecycleSpecificationSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var given in syntax.Given) VisitSpecificationEvent(given);
        if (syntax.WhenAppended is not null) VisitSpecificationEvent(syntax.WhenAppended);
        if (syntax.WhenStarted is not null) VisitExpression(syntax.WhenStarted);
        foreach (var outcome in syntax.Outcomes) VisitNode(outcome);
        foreach (var expectation in syntax.Then) VisitNode(expectation);
    }
}
