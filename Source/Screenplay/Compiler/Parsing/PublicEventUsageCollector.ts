// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from '../Syntax/Commands';
import { CaptureAppendSyntax, CaptureSourceSyntax } from '../Syntax/Captures';
import { ConstraintSyntax } from '../Syntax/Constraints';
import { dependencySourcesOf, ReducerRuleSyntax } from '../Syntax/DependencySources';
import { ReducerSyntax } from '../Syntax/ReducerSyntax';
import { consumedEvents, isEventsSource } from '../Syntax/CaptureEventsSource';
import { AllSyntax, JoinEventSyntax, ProjectionBlockSyntax, ProjectionSyntax } from '../Syntax/Projections';
import { ProducesSyntax, ReactionTriggerSyntax } from '../Syntax/Reactions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SpecificationSyntax } from '../Syntax/Specifications';
import { SliceSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';

/** Operational event edges only: fixtures and assertions are not subscriptions or appends. */
export class PublicEventUsageCollector extends ScreenplaySyntaxWalker {
    private command = false;
    readonly uses: { name: string; node: SyntaxNode; output: boolean; command: boolean }[] = [];
    readonly allEvents: AllSyntax[] = [];
    /** The '=> Name' of each projection and reducer: a read model, or an event in an outbound translation. */
    readonly projectionTargets: { target: string; node: SyntaxNode }[] = [];
    readonly eventSources: CaptureSourceSyntax[] = [];

    override visitProjection(syntax: ProjectionSyntax): void {
        if (syntax.readModel != null) this.projectionTargets.push({ target: syntax.readModel, node: syntax });
        super.visitProjection(syntax);
    }
    override visitReducer(syntax: ReducerSyntax): void {
        this.projectionTargets.push({ target: syntax.readModel, node: syntax });
        super.visitReducer(syntax);
    }
    override visitCaptureSource(syntax: CaptureSourceSyntax): void {
        if (isEventsSource(syntax)) {
            this.eventSources.push(syntax);
            consumedEvents(syntax).forEach(setting => this.uses.push({ name: setting.value, node: setting, output: false, command: false }));
        }
        super.visitCaptureSource(syntax);
    }

    override visitSlice(syntax: SliceSyntax): void {
        super.visitSlice(syntax);
        dependencySourcesOf(syntax).reducers?.forEach(reducer => this.visitReducer(reducer));
    }
    override visitCommand(syntax: CommandSyntax): void {
        this.command = true;
        super.visitCommand(syntax);
        this.command = false;
    }
    override visitProduces(syntax: ProducesSyntax): void {
        if (syntax.inlineOperation == null) this.uses.push({ name: syntax.event, node: syntax, output: true, command: this.command });
    }
    override visitCaptureAppend(syntax: CaptureAppendSyntax): void { this.inputOrOutput(syntax.event, syntax, true); }
    override visitJoinEvent(syntax: JoinEventSyntax): void { this.inputOrOutput(syntax.event, syntax); }
    override visitReducerRule(syntax: ReducerRuleSyntax): void { this.inputOrOutput(syntax.event, syntax); }
    override visitConstraint(syntax: ConstraintSyntax): void {
        syntax.releasedBy.forEach(name => this.inputOrOutput(name, syntax));
        if (syntax.kind !== 'FileConstraintSyntax') this.inputOrOutput(syntax.event, syntax);
        super.visitConstraint(syntax);
    }
    override visitProjectionBlock(syntax: ProjectionBlockSyntax): void {
        switch (syntax.kind) {
            case 'FromSyntax': syntax.events.forEach(event => this.inputOrOutput(event.event, event)); break;
            case 'ClearWithSyntax':
            case 'RemoveWithSyntax':
            case 'RemoveViaJoinSyntax': this.inputOrOutput(syntax.event, syntax); break;
            case 'AllSyntax': this.allEvents.push(syntax); break;
        }
        super.visitProjectionBlock(syntax);
    }
    override visitReactionTrigger(syntax: ReactionTriggerSyntax): void {
        if (syntax.source.kind === 'NamedTriggerSourceSyntax') this.inputOrOutput(syntax.source.name, syntax.source);
        super.visitReactionTrigger(syntax);
    }
    override visitSpecification(_syntax: SpecificationSyntax): void {}

    private inputOrOutput(name: string, node: SyntaxNode, output = false): void {
        this.uses.push({ name, node, output, command: false });
    }
}
