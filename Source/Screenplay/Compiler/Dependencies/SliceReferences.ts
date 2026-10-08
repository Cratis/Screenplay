// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PolicyReferenceSyntax } from '../Syntax/Authorization';
import { CommandSyntax } from '../Syntax/Commands';
import { ConstraintSyntax } from '../Syntax/Constraints';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { dependencySourcesOf, ConcurrencySyntax, FormPopulateViaQuerySyntax, FormSyntax, ReadsSyntax, ReducerRuleSyntax } from '../Syntax/DependencySources';
import { EventSpecSyntax, JoinEventSyntax, ProjectionSyntax, ProjectionEntersOnSyntax, RemoveWithSyntax, RemoveViaJoinSyntax, ClearWithSyntax } from '../Syntax/Projections';
import { InvokesSyntax, NamedTriggerSourceSyntax, ReactionTriggerSyntax } from '../Syntax/Reactions';
import { ScreenActionAlternativeSyntax, ScreenActionOtherwiseSyntax, ScreenActionSyntax, ScreenDataSyntax, ScreenNavigateSyntax } from '../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SpecificationSyntax, SpecificationEventSyntax, SpecificationCommandSyntax, SpecificationRedeliverySyntax } from '../Syntax/Specifications';
import { SliceSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import type { DependencyKind, SliceReference, SharedReference, CollectedSliceReferences } from './index';

export const referenceClassifications: Readonly<Record<string, DependencyKind | null>> = {
    from: 'usesFactsFrom', join: 'usesFactsFrom', remove: 'usesFactsFrom', removeViaJoin: 'usesFactsFrom',
    clear: 'usesFactsFrom', entersOn: 'usesFactsFrom', reduces: 'usesFactsFrom', uniqueEvent: 'usesFactsFrom',
    uniqueProperty: 'usesFactsFrom', concurrency: 'usesFactsFrom', trigger: 'reactsTo', reads: 'decidesFrom',
    invokes: 'asks', action: 'asks', actionAlternative: 'asks', actionOtherwise: 'asks', formCommand: 'asks', dataQuery: 'shows', populate: 'shows', navigate: 'shows',
    whenRedeliveredEvent: 'verifiedWith', redeliveryReaction: 'verifiedWith', refusalConstraint: null, refusalProduces: null,
    specificationEvent: 'verifiedWith', givenEvent: 'verifiedWith', whenAppendedEvent: 'verifiedWith', thenEvent: 'verifiedWith', whenCommand: 'verifiedWith',
    declares: null, uses: null, commandEventSource: null, commandStream: null, specificationEventSource: null, specificationStream: null, thenOperation: null,
    givenOperationFailure: null, thenCompensated: null, produces: null, authorizes: null, queryResult: null,
    dataReadModel: null, type: null, exampleType: null, contributes: null, template: null, specificationReadModel: null,
    thenAbsentReadModel: null, thenReadModel: null, givenReadModel: null, thenQuery: null,
    compositeKeyType: null, builds: null, buildsVariant: null, appends: null, seed: null,
};
// No resolution or expression/code interpretation: this is the same role inventory as C# SliceReferences.
export function sliceReferences(slice: SliceSyntax): CollectedSliceReferences {
    const collector = new SliceReferenceCollector();
    collector.visitSlice(slice);
    return { references: collector.references.sort((left, right) => left.location.line - right.location.line || left.location.column - right.location.column), shared: collector.shared };
}

export class SliceReferenceCollector extends ScreenplaySyntaxWalker {
    readonly references: SliceReference[] = [];
    readonly shared: SharedReference[] = [];
    private specification?: SpecificationSyntax;
    private projection = false;

    override visitSlice(syntax: SliceSyntax): void {
        super.visitSlice(syntax);
        const sources = dependencySourcesOf(syntax);
        sources.reducers?.forEach(node => this.visitReducer(node));
    }
    override visitCommand(syntax: CommandSyntax): void {
        const sources = dependencySourcesOf(syntax);
        sources.reads?.forEach(node => this.visitReads(node));
        if (sources.concurrency != null) this.visitConcurrency(sources.concurrency);
        super.visitCommand(syntax);
    }
    override visitReactionTrigger(syntax: ReactionTriggerSyntax): void {
        const sources = dependencySourcesOf(syntax);
        sources.data?.forEach(node => this.visitTriggerData(node));
        sources.reads?.forEach(node => this.visitReads(node));
        super.visitReactionTrigger(syntax);
    }
    override visitProjection(syntax: ProjectionSyntax): void {
        this.projection = true;
        super.visitProjection(syntax);
        this.projection = false;
    }
    override visitSpecification(syntax: SpecificationSyntax): void {
        this.specification = syntax;
        super.visitSpecification(syntax);
        this.specification = undefined;
    }
    override visitNode(node: SyntaxNode): void {
        const add = (name: string, targetKind: SliceReference['targetKind'], role: string) => {
            this.references.push({ name, targetKind, role, kind: referenceClassifications[role]!, location: node.location, timeline: this.projection || role === 'trigger' });
        };
        switch (node.kind) {
            case 'TypeRefSyntax': {
                const name = (node as TypeRefSyntax).name;
                if (!['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'].includes(name)) this.shared.push({ kind: 'type', name });
                break;
            }
            case 'PolicyReferenceSyntax': this.shared.push({ kind: 'policy', name: (node as PolicyReferenceSyntax).name }); break;
            case 'EventSpecSyntax': add((node as EventSpecSyntax).event, 'Event', 'from'); break;
            case 'JoinEventSyntax': add((node as JoinEventSyntax).event, 'Event', 'join'); break;
            case 'RemoveWithSyntax': add((node as RemoveWithSyntax).event, 'Event', 'remove'); break;
            case 'RemoveViaJoinSyntax': add((node as RemoveViaJoinSyntax).event, 'Event', 'removeViaJoin'); break;
            case 'ClearWithSyntax': add((node as ClearWithSyntax).event, 'Event', 'clear'); break;
            case 'ProjectionEntersOnSyntax': add((node as ProjectionEntersOnSyntax).event, 'Event', 'entersOn'); break;
            case 'ReducerRuleSyntax': add((node as ReducerRuleSyntax).event, 'Event', 'reduces'); break;
            case 'UniqueEventConstraintSyntax': add((node as Extract<ConstraintSyntax, { kind: 'UniqueEventConstraintSyntax' }>).event, 'Event', 'uniqueEvent'); break;
            case 'UniquePropertyConstraintSyntax': add((node as Extract<ConstraintSyntax, { kind: 'UniquePropertyConstraintSyntax' }>).event, 'Event', 'uniqueProperty'); break;
            case 'NamedTriggerSourceSyntax': add((node as NamedTriggerSourceSyntax).name, 'Event', 'trigger'); break;
            case 'ReadsSyntax': add((node as ReadsSyntax).readModel, 'ReadModel', 'reads'); break;
            case 'InvokesSyntax': add((node as InvokesSyntax).command, 'Command', 'invokes'); break;
            case 'ScreenActionSyntax': add((node as ScreenActionSyntax).command, 'Command', 'action'); break;
            case 'ScreenActionAlternativeSyntax': add((node as ScreenActionAlternativeSyntax).command, 'Command', 'actionAlternative'); break;
            case 'ScreenActionOtherwiseSyntax': {
                const command = (node as ScreenActionOtherwiseSyntax).command;
                if (command !== null) add(command, 'Command', 'actionOtherwise');
                break;
            }
            case 'FormSyntax': add((node as FormSyntax).for, 'Command', 'formCommand'); break;
            case 'ScreenDataSyntax': add((node as ScreenDataSyntax).query, 'Query', 'dataQuery'); break;
            case 'FormPopulateViaQuerySyntax': add((node as FormPopulateViaQuerySyntax).query, 'Query', 'populate'); break;
            case 'ScreenNavigateSyntax': add((node as ScreenNavigateSyntax).screen, 'Screen', 'navigate'); break;
            case 'SpecificationEventSyntax': add((node as SpecificationEventSyntax).eventType, 'Event', this.specification?.given.includes(node as SpecificationEventSyntax) === true ? 'givenEvent' : this.specification?.whenAppended === node ? 'whenAppendedEvent' : 'thenEvent'); break;
            case 'SpecificationCommandSyntax': add((node as SpecificationCommandSyntax).commandType, 'Command', 'whenCommand'); break;
            case 'SpecificationRedeliverySyntax': {
                const redelivery = node as SpecificationRedeliverySyntax;
                add(redelivery.eventType, 'Event', 'whenRedeliveredEvent');
                add(redelivery.reaction, 'Reaction', 'redeliveryReaction');
                break;
            }
            case 'ConcurrencySyntax': (node as ConcurrencySyntax).eventTypes.forEach(name => add(name, 'Event', 'concurrency')); break;
        }
    }
}
