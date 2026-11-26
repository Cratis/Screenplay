// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConditionSyntax } from './Conditions';
import { TypeRefSyntax } from './Declarations';
import { FileReferenceSyntax } from './Implementations';
import * as Interaction from './Interactions';
import { InteractionArgumentSyntax, ScreenBehaviorSyntax, ScreenComponentSyntax, ScreenDirectiveSyntax, ScreenTableSyntax, ScreenUsesBehaviorSyntax } from './Screens';
import { DialogTemplateSyntax, LayoutSyntax, ScreenTemplateSyntax } from './Layouts';
import { ApplicationSyntax, ModuleSyntax } from './Structure';
import { SyntaxNode } from './SyntaxNode';

/** The interaction portion of the syntax walker, sharing the enclosing walker's ordinary syntax hooks. */
export abstract class InteractionSyntaxWalker {
    abstract visitNode(syntax: SyntaxNode): void;
    abstract visitFileReference(syntax: FileReferenceSyntax): void;
    abstract visitTypeRef(syntax: TypeRefSyntax): void;
    abstract visitCondition(syntax: ConditionSyntax): void;
    abstract visitInteractionArgument(syntax: InteractionArgumentSyntax): void;
    abstract visitScreenDirective(syntax: ScreenDirectiveSyntax): void;
    protected abstract visitScreenComponentChildren(syntax: ScreenComponentSyntax): void;

    abstract visitLayout(syntax: LayoutSyntax): void;
    abstract visitScreenTemplate(syntax: ScreenTemplateSyntax): void;
    abstract visitDialogTemplate(syntax: DialogTemplateSyntax): void;

    protected visitApplicationInteractions(syntax: ApplicationSyntax): void {
        syntax.behaviors?.forEach(node => this.visitBehavior(node));
        syntax.layouts?.forEach(node => this.visitLayout(node));
    }

    protected visitModuleInteractions(syntax: ModuleSyntax): void {
        syntax.screenTemplates?.forEach(node => this.visitScreenTemplate(node));
        syntax.dialogTemplates?.forEach(node => this.visitDialogTemplate(node));
    }

    visitBehavior(syntax: Interaction.BehaviorSyntax): void {
        this.visitNode(syntax);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
        syntax.parameters.forEach(parameter => this.visitBehaviorParameter(parameter));
        syntax.bindings.forEach(binding => this.visitInteractionBinding(binding));
    }

    visitBehaviorParameter(syntax: Interaction.BehaviorParameterSyntax): void {
        this.visitNode(syntax);
        if (syntax.type !== null) this.visitTypeRef(syntax.type);
    }

    visitInteractionBinding(syntax: Interaction.InteractionBindingSyntax): void {
        this.visitNode(syntax);
        this.visitInteractionTrigger(syntax.trigger);
        syntax.alternatives.forEach(alternative => this.visitInteractionAlternative(alternative));
        if (syntax.otherwise !== null) this.visitInteractionOtherwise(syntax.otherwise);
        syntax.actions.forEach(action => this.visitInteractionAction(action));
    }

    visitInteractionAlternative(syntax: Interaction.InteractionAlternativeSyntax): void {
        this.visitNode(syntax);
        this.visitCondition(syntax.condition);
        syntax.actions.forEach(action => this.visitInteractionAction(action));
    }

    visitInteractionOtherwise(syntax: Interaction.InteractionOtherwiseSyntax): void {
        this.visitNode(syntax);
        syntax.actions.forEach(action => this.visitInteractionAction(action));
    }

    visitInteractionTrigger(syntax: Interaction.InteractionTriggerSyntax): void {
        switch (syntax.kind) {
            case 'BuiltInInteractionTriggerSyntax': this.visitBuiltInInteractionTrigger(syntax); break;
            case 'EventInteractionTriggerSyntax': this.visitEventInteractionTrigger(syntax); break;
            case 'IntervalInteractionTriggerSyntax': this.visitIntervalInteractionTrigger(syntax); break;
            case 'ApplicationTriggerInteractionTriggerSyntax': this.visitApplicationTriggerInteractionTrigger(syntax); break;
            default: this.visitNode(syntax); break;
        }
    }

    visitBuiltInInteractionTrigger(syntax: Interaction.BuiltInInteractionTriggerSyntax): void { this.visitNode(syntax); }
    visitEventInteractionTrigger(syntax: Interaction.EventInteractionTriggerSyntax): void { this.visitNode(syntax); }
    visitIntervalInteractionTrigger(syntax: Interaction.IntervalInteractionTriggerSyntax): void { this.visitNode(syntax); }
    visitApplicationTriggerInteractionTrigger(syntax: Interaction.ApplicationTriggerInteractionTriggerSyntax): void { this.visitNode(syntax); }

    visitInteractionAction(syntax: Interaction.InteractionActionSyntax): void {
        if (!this.visitTargetedInteractionAction(syntax)) this.visitLocalInteractionAction(syntax);
        syntax.arguments.forEach(argument => this.visitInteractionArgument(argument));
        syntax.onSuccess.forEach(action => this.visitInteractionAction(action));
        syntax.onFailure.forEach(action => this.visitInteractionAction(action));
        syntax.onResult.forEach(action => this.visitInteractionAction(action));
    }

    private visitTargetedInteractionAction(syntax: Interaction.InteractionActionSyntax): boolean {
        switch (syntax.kind) {
            case 'ExecuteCommandActionSyntax': this.visitExecuteCommandAction(syntax); return true;
            case 'NavigateActionSyntax': this.visitNavigateAction(syntax); return true;
            case 'OpenDialogActionSyntax': this.visitOpenDialogAction(syntax); return true;
            case 'RefreshQueryActionSyntax': this.visitRefreshQueryAction(syntax); return true;
            case 'RaiseTriggerActionSyntax': this.visitRaiseTriggerAction(syntax); return true;
            default: return false;
        }
    }

    private visitLocalInteractionAction(syntax: Interaction.InteractionActionSyntax): void {
        switch (syntax.kind) {
            case 'NavigateBackActionSyntax': this.visitNavigateBackAction(syntax); break;
            case 'CloseDialogActionSyntax': this.visitCloseDialogAction(syntax); break;
            case 'SetStateActionSyntax': this.visitSetStateAction(syntax); break;
            case 'NotifyActionSyntax': this.visitNotifyAction(syntax); break;
            case 'ConfirmActionSyntax': this.visitConfirmAction(syntax); break;
            default: this.visitNode(syntax); break;
        }
    }

    visitExecuteCommandAction(syntax: Interaction.ExecuteCommandActionSyntax): void { this.visitNode(syntax); }
    visitNavigateAction(syntax: Interaction.NavigateActionSyntax): void { this.visitNode(syntax); }
    visitNavigateBackAction(syntax: Interaction.NavigateBackActionSyntax): void { this.visitNode(syntax); }
    visitOpenDialogAction(syntax: Interaction.OpenDialogActionSyntax): void { this.visitNode(syntax); }
    visitCloseDialogAction(syntax: Interaction.CloseDialogActionSyntax): void { this.visitNode(syntax); }
    visitRefreshQueryAction(syntax: Interaction.RefreshQueryActionSyntax): void { this.visitNode(syntax); }
    visitSetStateAction(syntax: Interaction.SetStateActionSyntax): void { this.visitNode(syntax); }
    visitNotifyAction(syntax: Interaction.NotifyActionSyntax): void { this.visitNode(syntax); }
    visitConfirmAction(syntax: Interaction.ConfirmActionSyntax): void { this.visitNode(syntax); }
    visitRaiseTriggerAction(syntax: Interaction.RaiseTriggerActionSyntax): void { this.visitNode(syntax); }

    visitUsesBehavior(syntax: Interaction.UsesBehaviorSyntax): void {
        this.visitNode(syntax);
        syntax.arguments.forEach(argument => this.visitBehaviorArgument(argument));
    }

    visitBehaviorArgument(syntax: Interaction.BehaviorArgumentSyntax): void { this.visitNode(syntax); }

    visitAttachments(behaviors: readonly Interaction.BehaviorSyntax[], usedBehaviors: readonly Interaction.UsesBehaviorSyntax[]): void {
        behaviors.forEach(behavior => this.visitBehavior(behavior));
        usedBehaviors.forEach(uses => this.visitUsesBehavior(uses));
    }

    visitScreenBehavior(syntax: ScreenBehaviorSyntax): void {
        this.visitNode(syntax);
        this.visitBehavior(syntax.behavior);
    }

    visitScreenUsesBehavior(syntax: ScreenUsesBehaviorSyntax): void {
        this.visitNode(syntax);
        this.visitUsesBehavior(syntax.uses);
    }

    visitScreenTable(syntax: ScreenTableSyntax): void {
        this.visitNode(syntax);
        syntax.columns.forEach(column => this.visitNode(column));
        if (syntax.rowClick !== null) this.visitScreenDirective(syntax.rowClick);
        this.visitAttachments(syntax.behaviors ?? [], syntax.usedBehaviors ?? []);
    }

    visitScreenComponent(syntax: ScreenComponentSyntax): void {
        this.visitNode(syntax);
        this.visitScreenComponentChildren(syntax);
        this.visitAttachments(syntax.behaviors ?? [], syntax.usedBehaviors ?? []);
    }

    protected visitInteractionDirective(syntax: ScreenDirectiveSyntax): boolean {
        switch (syntax.kind) {
            case 'ScreenBehaviorSyntax': this.visitScreenBehavior(syntax); return true;
            case 'ScreenUsesBehaviorSyntax': this.visitScreenUsesBehavior(syntax); return true;
            case 'ScreenTableSyntax': this.visitScreenTable(syntax); return true;
            case 'ScreenComponentSyntax': this.visitScreenComponent(syntax); return true;
            default: return false;
        }
    }
}
