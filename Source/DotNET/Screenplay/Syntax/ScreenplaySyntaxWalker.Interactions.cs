// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="BehaviorSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="BehaviorSyntax"/> to visit.</param>
    public virtual void VisitBehavior(BehaviorSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        foreach (var parameter in syntax.Parameters)
        {
            VisitBehaviorParameter(parameter);
        }

        foreach (var binding in syntax.Bindings)
        {
            VisitInteractionBinding(binding);
        }
    }

    /// <summary>
    /// Visits a <see cref="BehaviorParameterSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="BehaviorParameterSyntax"/> to visit.</param>
    public virtual void VisitBehaviorParameter(BehaviorParameterSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Type is not null)
        {
            VisitTypeRef(syntax.Type);
        }
    }

    /// <summary>
    /// Visits an <see cref="InteractionBindingSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="InteractionBindingSyntax"/> to visit.</param>
    public virtual void VisitInteractionBinding(InteractionBindingSyntax syntax)
    {
        VisitNode(syntax);
        VisitInteractionTrigger(syntax.Trigger);

        foreach (var action in syntax.Actions)
        {
            VisitInteractionAction(action);
        }
    }

    /// <summary>
    /// Visits an <see cref="InteractionTriggerSyntax"/> node, dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="InteractionTriggerSyntax"/> to visit.</param>
    /// <remarks>
    /// The <c>default</c> arm keeps a consumer of an older version walking a document that carries a trigger
    /// kind it does not know about, per the AST compatibility rule.
    /// </remarks>
    public virtual void VisitInteractionTrigger(InteractionTriggerSyntax syntax)
    {
        switch (syntax)
        {
            case BuiltInInteractionTriggerSyntax builtIn:
                VisitBuiltInInteractionTrigger(builtIn);
                break;
            case EventInteractionTriggerSyntax @event:
                VisitEventInteractionTrigger(@event);
                break;
            case IntervalInteractionTriggerSyntax interval:
                VisitIntervalInteractionTrigger(interval);
                break;
            case ApplicationTriggerInteractionTriggerSyntax applicationTrigger:
                VisitApplicationTriggerInteractionTrigger(applicationTrigger);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="BuiltInInteractionTriggerSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="BuiltInInteractionTriggerSyntax"/> to visit.</param>
    public virtual void VisitBuiltInInteractionTrigger(BuiltInInteractionTriggerSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="EventInteractionTriggerSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="EventInteractionTriggerSyntax"/> to visit.</param>
    public virtual void VisitEventInteractionTrigger(EventInteractionTriggerSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="IntervalInteractionTriggerSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="IntervalInteractionTriggerSyntax"/> to visit.</param>
    public virtual void VisitIntervalInteractionTrigger(IntervalInteractionTriggerSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="ApplicationTriggerInteractionTriggerSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ApplicationTriggerInteractionTriggerSyntax"/> to visit.</param>
    public virtual void VisitApplicationTriggerInteractionTrigger(ApplicationTriggerInteractionTriggerSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="InteractionActionSyntax"/> node, dispatching to the method for its kind, then its
    /// arguments and continuations.
    /// </summary>
    /// <param name="syntax">The <see cref="InteractionActionSyntax"/> to visit.</param>
    /// <remarks>
    /// The <c>default</c> arm keeps a consumer of an older version walking a document that carries an action
    /// kind it does not know about, per the AST compatibility rule. Arguments and continuations are visited for
    /// every kind, including an unknown one, so a walker never loses the tree beneath what it did not recognise.
    /// </remarks>
    public virtual void VisitInteractionAction(InteractionActionSyntax syntax)
    {
        switch (syntax)
        {
            case ExecuteCommandActionSyntax execute:
                VisitExecuteCommandAction(execute);
                break;
            case NavigateActionSyntax navigate:
                VisitNavigateAction(navigate);
                break;
            case NavigateBackActionSyntax navigateBack:
                VisitNavigateBackAction(navigateBack);
                break;
            case OpenDialogActionSyntax openDialog:
                VisitOpenDialogAction(openDialog);
                break;
            case CloseDialogActionSyntax closeDialog:
                VisitCloseDialogAction(closeDialog);
                break;
            case RefreshQueryActionSyntax refresh:
                VisitRefreshQueryAction(refresh);
                break;
            case SetStateActionSyntax set:
                VisitSetStateAction(set);
                break;
            case NotifyActionSyntax notify:
                VisitNotifyAction(notify);
                break;
            case ConfirmActionSyntax confirm:
                VisitConfirmAction(confirm);
                break;
            case RaiseTriggerActionSyntax raise:
                VisitRaiseTriggerAction(raise);
                break;
            default:
                VisitNode(syntax);
                break;
        }

        foreach (var argument in syntax.Arguments)
        {
            VisitInteractionArgument(argument);
        }

        foreach (var action in syntax.OnSuccess)
        {
            VisitInteractionAction(action);
        }

        foreach (var action in syntax.OnFailure)
        {
            VisitInteractionAction(action);
        }

        foreach (var action in syntax.OnResult)
        {
            VisitInteractionAction(action);
        }
    }

    /// <summary>
    /// Visits an <see cref="InteractionArgumentSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="InteractionArgumentSyntax"/> to visit.</param>
    public virtual void VisitInteractionArgument(InteractionArgumentSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="ExecuteCommandActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ExecuteCommandActionSyntax"/> to visit.</param>
    public virtual void VisitExecuteCommandAction(ExecuteCommandActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="NavigateActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigateActionSyntax"/> to visit.</param>
    public virtual void VisitNavigateAction(NavigateActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="NavigateBackActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigateBackActionSyntax"/> to visit.</param>
    public virtual void VisitNavigateBackAction(NavigateBackActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="OpenDialogActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="OpenDialogActionSyntax"/> to visit.</param>
    public virtual void VisitOpenDialogAction(OpenDialogActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CloseDialogActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CloseDialogActionSyntax"/> to visit.</param>
    public virtual void VisitCloseDialogAction(CloseDialogActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="RefreshQueryActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="RefreshQueryActionSyntax"/> to visit.</param>
    public virtual void VisitRefreshQueryAction(RefreshQueryActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SetStateActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SetStateActionSyntax"/> to visit.</param>
    public virtual void VisitSetStateAction(SetStateActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="NotifyActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="NotifyActionSyntax"/> to visit.</param>
    public virtual void VisitNotifyAction(NotifyActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ConfirmActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ConfirmActionSyntax"/> to visit.</param>
    public virtual void VisitConfirmAction(ConfirmActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="RaiseTriggerActionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="RaiseTriggerActionSyntax"/> to visit.</param>
    public virtual void VisitRaiseTriggerAction(RaiseTriggerActionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="UsesBehaviorSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="UsesBehaviorSyntax"/> to visit.</param>
    public virtual void VisitUsesBehavior(UsesBehaviorSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var argument in syntax.Arguments)
        {
            VisitBehaviorArgument(argument);
        }
    }

    /// <summary>
    /// Visits a <see cref="BehaviorArgumentSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="BehaviorArgumentSyntax"/> to visit.</param>
    public virtual void VisitBehaviorArgument(BehaviorArgumentSyntax syntax) => VisitNode(syntax);
}
