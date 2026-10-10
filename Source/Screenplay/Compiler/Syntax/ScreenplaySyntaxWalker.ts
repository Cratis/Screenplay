// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentitySyntax } from './IdentitySyntax';
import { IdentityDetailSyntax } from './IdentityDetailSyntax';
import { IdentitySourceSyntax } from './IdentitySourceSyntax';
import { ClaimIdentitySourceSyntax } from './ClaimIdentitySourceSyntax';
import { QueryIdentitySourceSyntax } from './QueryIdentitySourceSyntax';
import { CodeIdentitySourceSyntax } from './CodeIdentitySourceSyntax';
import { FileIdentitySourceSyntax } from './FileIdentitySourceSyntax';
import { AuthorizeSyntax, PersonaSyntax, PolicyRequirementSyntax } from './Authorization';
import { CaptureAppendSyntax, CaptureChildrenSyntax, CaptureMapOperationSyntax, CaptureNestedSyntax, CaptureSourceSettingSyntax, CaptureSourceSyntax, CaptureSyntax } from './Captures';
import { CommandSyntax, ValidateSyntax, ValidationRuleSyntax } from './Commands';
import { ConstraintSyntax } from './Constraints';
import { ConditionSyntax } from './Conditions';
import { PolicyConditionSyntax, PolicySyntax } from './Policies';
import { SeedSyntax } from './Seeds';
import { ConceptSyntax, DomainSyntax, EventSyntax, ImportSyntax, PropertySyntax, ReadModelSyntax, TagSyntax, TypeRefSyntax, TypeSyntax } from './Declarations';
import { IdentityExpressionSyntax, ExpressionSyntax, ObjectMemberSyntax, PropertyMappingSyntax } from './Expressions';
import { JoinEventSyntax, KeySyntax, MappingSyntax, ProjectionBlockSyntax, ProjectionSyntax } from './Projections';
import { CommandStreamSyntax, EventSourceSyntax, EventStreamSyntax } from './EventSources';
import { QueryParameterSyntax, QuerySyntax } from './Queries';
import { InvocationRefusalSyntax } from './InvocationRefusalSyntax';
import { SpecificationRedeliverySyntax } from './SpecificationRedeliverySyntax';
import { InvokesSyntax, ProducesSyntax, ReactionSyntax, ReactionTriggerSyntax, TriggerSourceSyntax } from './Reactions';
import { InteractionArgumentSyntax, ScreenActionAlternativeSyntax, ScreenActionOtherwiseSyntax, ScreenDirectiveSyntax, ScreenGuardedActionSyntax, ScreenSyntax } from './Screens';
import {
    SpecificationExampleSyntax, SpecificationCaptureSyntax, SpecificationClockSyntax, SpecificationCommandSyntax, SpecificationEventSyntax, SpecificationQueryResultSyntax, SpecificationStreamSyntax, SpecificationNoStreamSyntax,
    SpecificationReadModelSyntax, SpecificationOperationFailureSyntax, SpecificationOperationSyntax, SpecificationCompensatedSyntax, SpecificationSyntax, SpecificationTriggerSyntax, SpecificationWhenQuerySyntax,
} from './Specifications';
import { ApplicationSyntax, DependsOnSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax, SliceSyntax } from './Structure';
import { CommandResponseSyntax, PropertyResponseSourceSyntax, RecordCommandResponseSyntax, RecordSpecificationReturnSyntax, ResponseFieldSyntax, ScalarCommandResponseSyntax, ScalarSpecificationReturnSyntax, SpecificationReturnSyntax } from './Responses';
import { EventStreamIdPartSyntax } from './EventStreamIdPartSyntax';
import { SyntaxNode } from './SyntaxNode';
import { PurposeSyntax, PurposeReferenceSyntax, PurposeTransferSyntax } from './Purposes';
import { OperationSyntax, OperationPhaseSyntax, SystemSyntax } from './Operations';
import { CodeBlockSyntax, FileReferenceSyntax, HandlerSyntax, ImplementationSyntax, ImplementationHintSyntax } from './Implementations';
import { ReadsSyntax, ConcurrencySyntax, ReducerSyntax, ReducerRuleSyntax, FormSyntax, TriggerDataSyntax } from './DependencySources';

// Walks a whole syntax tree, depth first, in the order the C# ScreenplaySyntaxWalker does. Every visit
// method calls visitNode and then walks the node's children, so an emitter overrides only the nodes it
// cares about - and calls the base method when it still wants the children walked.
export abstract class ScreenplaySyntaxWalker {
    visitNode(_node: SyntaxNode): void {}

    visitApplication(syntax: ApplicationSyntax): void {
        this.visitNode(syntax);
        if (syntax.domain !== null) this.visitDomain(syntax.domain);
        syntax.imports.forEach(node => this.visitImport(node));
        syntax.fileImports.forEach(node => this.visitFileImport(node));
        syntax.systems?.forEach(node => this.visitSystem(node));
        syntax.eventSources?.forEach(node => this.visitEventSource(node));
        syntax.concepts.forEach(node => this.visitConcept(node));
        syntax.types.forEach(node => this.visitType(node));
        syntax.personas.forEach(node => this.visitPersona(node));
        syntax.policies?.forEach(node => this.visitPolicy(node));
        syntax.exposures?.forEach(node => {
            this.visitNode(node);
            node.properties.forEach(property => this.visitNode(property));
        });
        syntax.instanceContributions?.forEach(node => {
            this.visitNode(node);
            node.contributions.forEach(contribution => {
                this.visitNode(contribution);
                if (contribution.value !== null) this.visitExpression(contribution.value);
                contribution.items.forEach(item => {
                    this.visitNode(item);
                    item.values.forEach(value => {
                        this.visitNode(value);
                        this.visitExpression(value.value);
                    });
                });
            });
        });
        if (syntax.identity != null) this.visitIdentity(syntax.identity);
        syntax.purposes?.forEach(node => this.visitPurpose(node));
        syntax.seeds?.forEach(node => this.visitSeed(node));
        syntax.modules.forEach(node => this.visitModule(node));
        syntax.examples?.forEach(node => this.visitSpecificationExample(node));
    }

    visitIdentity(syntax: IdentitySyntax): void {
        this.visitNode(syntax);
        syntax.details.forEach(detail => this.visitIdentityDetail(detail));
    }

    visitIdentityDetail(syntax: IdentityDetailSyntax): void {
        this.visitNode(syntax);
        this.visitTypeRef(syntax.type);
        this.visitIdentitySource(syntax.source);
    }

    visitIdentitySource(syntax: IdentitySourceSyntax): void {
        switch (syntax.kind) {
            case 'ClaimIdentitySourceSyntax': this.visitClaimIdentitySource(syntax); break;
            case 'QueryIdentitySourceSyntax': this.visitQueryIdentitySource(syntax); break;
            case 'CodeIdentitySourceSyntax': this.visitCodeIdentitySource(syntax); break;
            case 'FileIdentitySourceSyntax': this.visitFileIdentitySource(syntax); break;
            default: this.visitNode(syntax); break;
        }
    }

    visitClaimIdentitySource(syntax: ClaimIdentitySourceSyntax): void { this.visitNode(syntax); }
    visitQueryIdentitySource(syntax: QueryIdentitySourceSyntax): void { this.visitNode(syntax); this.visitExpression(syntax.by); }
    visitCodeIdentitySource(syntax: CodeIdentitySourceSyntax): void { this.visitNode(syntax); this.visitCodeBlock(syntax.code); }
    visitFileIdentitySource(syntax: FileIdentitySourceSyntax): void { this.visitNode(syntax); this.visitFileReference(syntax.file); }

    visitPurpose(syntax: PurposeSyntax): void {
        this.visitNode(syntax);
        syntax.transfers.forEach(node => this.visitPurposeTransfer(node));
    }

    visitPurposeReference(syntax: PurposeReferenceSyntax): void { this.visitNode(syntax); }
    visitPurposeTransfer(syntax: PurposeTransferSyntax): void { this.visitNode(syntax); }

    visitPolicy(syntax: PolicySyntax): void {
        this.visitNode(syntax);
        if (syntax.condition !== null) this.visitPolicyCondition(syntax.condition);
        if (syntax.code !== null) this.visitCodeBlock(syntax.code);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
    }

    visitPolicyCondition(syntax: PolicyConditionSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'ClaimConditionSyntax' && syntax.matches !== null) this.visitExpression(syntax.matches);
        if (syntax.kind === 'NotPolicyConditionSyntax') this.visitPolicyCondition(syntax.operand);
        if (syntax.kind === 'LogicalPolicyConditionSyntax') { this.visitPolicyCondition(syntax.left); this.visitPolicyCondition(syntax.right); }
    }

    visitSeed(syntax: SeedSyntax): void {
        this.visitNode(syntax);
        syntax.groups.forEach(group => {
            this.visitNode(group);
            group.events.forEach(event => { this.visitNode(event); event.properties.forEach(property => this.visitPropertyMapping(property)); });
        });
    }

    visitPersona(syntax: PersonaSyntax): void {
        this.visitNode(syntax);
    }

    visitAuthorize(syntax: AuthorizeSyntax): void {
        this.visitNode(syntax);
        this.visitPolicyRequirement(syntax.requirement);
    }

    visitPolicyRequirement(syntax: PolicyRequirementSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'LogicalPolicyRequirementSyntax') {
            this.visitPolicyRequirement(syntax.left);
            this.visitPolicyRequirement(syntax.right);
        }
    }

    visitDomain(syntax: DomainSyntax): void {
        this.visitNode(syntax);
    }

    visitImport(syntax: ImportSyntax): void {
        this.visitNode(syntax);
    }

    visitFileImport(syntax: FileImportSyntax): void {
        this.visitNode(syntax);
    }

    visitConcept(syntax: ConceptSyntax): void {
        this.visitNode(syntax);
        syntax.attributes.forEach(node => this.visitNode(node));
        syntax.validations?.forEach(node => this.visitValidate(node));
    }

    visitType(syntax: TypeSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
    }

    visitDependsOn(syntax: DependsOnSyntax): void { this.visitNode(syntax); }

    visitModule(syntax: ModuleSyntax): void {
        this.visitNode(syntax);
        syntax.purposes?.forEach(node => this.visitPurposeReference(node));
        syntax.examples?.forEach(node => this.visitSpecificationExample(node));
        syntax.dependsOn?.forEach(node => this.visitDependsOn(node));
        syntax.fileImports.forEach(node => this.visitFileImport(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        syntax.features.forEach(node => this.visitFeature(node));
    }

    visitFeature(syntax: FeatureSyntax): void {
        this.visitNode(syntax);
        syntax.purposes?.forEach(node => this.visitPurposeReference(node));
        syntax.examples?.forEach(node => this.visitSpecificationExample(node));
        syntax.dependsOn?.forEach(node => this.visitDependsOn(node));
        syntax.fileImports.forEach(node => this.visitFileImport(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        syntax.features.forEach(node => this.visitFeature(node));
        syntax.slices.forEach(node => this.visitSlice(node));
    }

    visitSlice(syntax: SliceSyntax): void {
        this.visitNode(syntax);
        syntax.purposes?.forEach(node => this.visitPurposeReference(node));
        syntax.examples?.forEach(node => this.visitSpecificationExample(node));
        syntax.commands.forEach(node => this.visitCommand(node));
        syntax.operations?.forEach(node => this.visitOperation(node));
        syntax.events.forEach(node => this.visitEvent(node));
        syntax.constraints.forEach(node => this.visitConstraint(node));
        syntax.queries.forEach(node => this.visitQuery(node));
        syntax.projections.forEach(node => this.visitProjection(node));
        syntax.readModels.forEach(node => this.visitReadModel(node));
        syntax.captures.forEach(node => this.visitCapture(node));
        syntax.reactions.forEach(node => this.visitReaction(node));
        syntax.screens.forEach(node => this.visitScreen(node));
        syntax.specifications.forEach(node => this.visitSpecification(node));
    }

    // Explicit reference-only entry points. Default tree traversal remains the narrowed wire traversal;
    // dependency consumers opt into parser-owned side-table payloads rather than changing board visitors.
    visitTriggerData(syntax: TriggerDataSyntax): void {
        this.visitNode(syntax);
        if (syntax.type !== null) this.visitTypeRef(syntax.type);
    }
    visitReads(syntax: ReadsSyntax): void { this.visitNode(syntax); }
    visitConcurrency(syntax: ConcurrencySyntax): void { this.visitNode(syntax); }
    visitReducer(syntax: ReducerSyntax): void {
        this.visitNode(syntax);
        syntax.rules.forEach(node => this.visitReducerRule(node));
    }
    visitReducerRule(syntax: ReducerRuleSyntax): void {
        this.visitNode(syntax);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
        if (syntax.code !== null) this.visitCodeBlock(syntax.code);
    }
    visitForm(syntax: FormSyntax): void {
        this.visitNode(syntax);
        if (syntax.populate !== null) this.visitNode(syntax.populate);
        syntax.fields.forEach(node => this.visitNode(node));
        syntax.columns?.forEach(node => this.visitNode(node));
        if (syntax.layout !== null && syntax.layout !== undefined) {
            this.visitNode(syntax.layout);
            syntax.layout.columns.forEach(column => {
                this.visitNode(column);
                if (column.width !== null) this.visitNode(column.width);
                if (column.minWidth !== null) this.visitNode(column.minWidth);
                if (column.maxWidth !== null) this.visitNode(column.maxWidth);
            });
            if (syntax.layout.columnGap !== null) this.visitNode(syntax.layout.columnGap);
            if (syntax.layout.rowGap !== null) this.visitNode(syntax.layout.rowGap);
            syntax.layout.placements.forEach(placement => {
                this.visitNode(placement);
                if (placement.width !== null) this.visitNode(placement.width);
            });
        }
        if (syntax.onSubmit !== null) this.visitScreenDirective(syntax.onSubmit);
    }

    visitCommand(syntax: CommandSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        if (syntax.stream != null) this.visitCommandStream(syntax.stream);
        syntax.streamCandidates?.forEach(candidate => this.visitCommandStream(candidate));
        syntax.validations.forEach(node => this.visitValidate(node));
        syntax.produces.forEach(node => this.visitProduces(node));
        if (syntax.handler != null) this.visitHandler(syntax.handler);
        if (syntax.response != null) this.visitCommandResponse(syntax.response);
    }

    visitEventSource(syntax: EventSourceSyntax): void {
        this.visitNode(syntax);
        if (syntax.identifier !== null) this.visitTypeRef(syntax.identifier);
        syntax.streams.forEach(stream => this.visitEventStream(stream));
    }
    visitEventStream(syntax: EventStreamSyntax): void {
        this.visitNode(syntax);
        if (syntax.streamId !== null) this.visitTypeRef(syntax.streamId);
        syntax.streamIdParts.forEach(part => this.visitEventStreamIdPart(part));
    }
    visitEventStreamIdPart(syntax: EventStreamIdPartSyntax): void {
        this.visitNode(syntax);
        this.visitTypeRef(syntax.type);
    }
    visitCommandStream(syntax: CommandStreamSyntax): void {
        this.visitNode(syntax);
        if (syntax.streamId !== null) this.visitPropertyMapping(syntax.streamId);
        syntax.streamIdParts.forEach(part => this.visitPropertyMapping(part));
        if (syntax.propertyCandidate !== null) this.visitProperty(syntax.propertyCandidate);
    }

    visitSystem(syntax: SystemSyntax): void { this.visitNode(syntax); }
    visitOperation(syntax: OperationSyntax): void {
        this.visitNode(syntax);
        syntax.inputs.forEach(input => this.visitProperty(input));
        if (syntax.execute !== null) this.visitOperationPhase(syntax.execute);
        if (syntax.compensate !== null) this.visitOperationPhase(syntax.compensate);
    }
    visitOperationPhase(syntax: OperationPhaseSyntax): void {
        this.visitNode(syntax);
        if (syntax.implementation !== null) this.visitImplementation(syntax.implementation);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
        if (syntax.code !== null) this.visitCodeBlock(syntax.code);
    }
    visitSpecificationOperationFailure(syntax: SpecificationOperationFailureSyntax): void { this.visitNode(syntax); }
    visitSpecificationCompensated(syntax: SpecificationCompensatedSyntax): void { this.visitNode(syntax); }
    visitSpecificationOperation(syntax: SpecificationOperationSyntax): void {
        this.visitNode(syntax);
        syntax.values.forEach(value => this.visitPropertyMapping(value));
    }

    visitHandler(syntax: HandlerSyntax): void {
        this.visitNode(syntax);
        if (syntax.implementation != null) this.visitImplementation(syntax.implementation);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
        if (syntax.code !== null) this.visitCodeBlock(syntax.code);
    }

    visitImplementation(syntax: ImplementationSyntax): void {
        this.visitNode(syntax);
        syntax.hints.forEach(hint => this.visitImplementationHint(hint));
    }

    visitImplementationHint(syntax: ImplementationHintSyntax): void { this.visitNode(syntax); }
    visitFileReference(syntax: FileReferenceSyntax): void { this.visitNode(syntax); }
    visitCodeBlock(syntax: CodeBlockSyntax): void { this.visitNode(syntax); }

    visitCommandResponse(syntax: CommandResponseSyntax): void {
        if (syntax.kind === 'ScalarCommandResponseSyntax') this.visitScalarCommandResponse(syntax);
        else this.visitRecordCommandResponse(syntax);
    }

    visitScalarCommandResponse(syntax: ScalarCommandResponseSyntax): void {
        this.visitNode(syntax);
        this.visitPropertyResponseSource(syntax.source);
    }

    visitRecordCommandResponse(syntax: RecordCommandResponseSyntax): void {
        this.visitNode(syntax);
        syntax.fields.forEach(field => this.visitResponseField(field));
    }

    visitResponseField(syntax: ResponseFieldSyntax): void {
        this.visitNode(syntax);
        if (syntax.type !== null) this.visitTypeRef(syntax.type);
        this.visitPropertyResponseSource(syntax.source);
    }

    visitPropertyResponseSource(syntax: PropertyResponseSourceSyntax): void {
        this.visitNode(syntax);
    }

    visitSpecificationReturn(syntax: SpecificationReturnSyntax): void {
        if (syntax.kind === 'ScalarSpecificationReturnSyntax') this.visitScalarSpecificationReturn(syntax);
        else this.visitRecordSpecificationReturn(syntax);
    }

    visitScalarSpecificationReturn(syntax: ScalarSpecificationReturnSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.value);
    }

    visitRecordSpecificationReturn(syntax: RecordSpecificationReturnSyntax): void {
        this.visitNode(syntax);
        syntax.fields.forEach(field => this.visitPropertyMapping(field));
    }

    visitValidate(syntax: ValidateSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'DeclarativeValidateSyntax') {
            syntax.rules.forEach(node => this.visitValidationRule(node));
            syntax.requirements?.forEach(node => { this.visitNode(node); this.visitCondition(node.condition); });
        } else if (syntax.code != null) this.visitCodeBlock(syntax.code);
    }

    visitValidationRule(syntax: ValidationRuleSyntax): void {
        this.visitNode(syntax);
        if (syntax.implementation != null) this.visitImplementation(syntax.implementation);
        if (syntax.value !== null) this.visitExpression(syntax.value);
        if (syntax.file !== null) this.visitFileReference(syntax.file);
        if (syntax.code !== null) this.visitCodeBlock(syntax.code);
    }

    visitEvent(syntax: EventSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
        syntax.tags.forEach(node => this.visitTag(node));
    }

    visitTag(syntax: TagSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.value);
    }

    visitReadModel(syntax: ReadModelSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
    }

    visitConstraint(syntax: ConstraintSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'FileConstraintSyntax') this.visitNode(syntax.file);
        syntax.additionalRules.forEach(node => this.visitConstraint(node));
    }

    visitQuery(syntax: QuerySyntax): void {
        this.visitNode(syntax);
        this.visitTypeRef(syntax.returnType);
        if (syntax.by !== null) this.visitQueryParameter(syntax.by);
        syntax.filters.forEach(node => this.visitQueryParameter(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
    }

    visitQueryParameter(syntax: QueryParameterSyntax): void {
        this.visitNode(syntax);
        this.visitTypeRef(syntax.type);
        if (syntax.source != null) this.visitExpression(syntax.source);
    }

    visitKey(syntax: KeySyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'ExpressionKeySyntax') this.visitExpression(syntax.expression);
        else syntax.parts.forEach(part => { this.visitNode(part); this.visitExpression(part.expression); });
    }

    visitProjection(syntax: ProjectionSyntax): void {
        this.visitNode(syntax);
        if (syntax.key != null) this.visitKey(syntax.key);
        syntax.blocks.forEach(node => this.visitProjectionBlock(node));
    }

    visitProjectionBlock(syntax: ProjectionBlockSyntax): void {
        this.visitNode(syntax);
        switch (syntax.kind) {
            case 'FromSyntax':
                syntax.events.forEach(node => { this.visitNode(node); if (node.key != null) this.visitExpression(node.key); });
                if (syntax.key != null) this.visitKey(syntax.key);
                if (syntax.parentKey != null) this.visitExpression(syntax.parentKey);
                syntax.mappings.forEach(node => this.visitMapping(node));
                break;
            case 'EverySyntax':
            case 'AllSyntax':
                syntax.mappings.forEach(node => this.visitMapping(node));
                break;
            case 'JoinSyntax':
                syntax.events.forEach(node => this.visitJoinEvent(node));
                break;
            case 'ProjectionVariantSyntax':
                syntax.entersOn.forEach(node => { this.visitNode(node); if (node.key != null) this.visitExpression(node.key); });
                syntax.blocks.forEach(node => this.visitProjectionBlock(node));
                break;
            case 'ChildrenSyntax':
                if (syntax.identifiedBy !== undefined) this.visitExpression(syntax.identifiedBy);
                syntax.blocks.forEach(node => this.visitProjectionBlock(node));
                break;
            case 'NestedSyntax':
                syntax.blocks.forEach(node => this.visitProjectionBlock(node));
                break;
            case 'RemoveWithSyntax':
                if (syntax.parentKey != null) this.visitExpression(syntax.parentKey);
                if (syntax.key != null) this.visitExpression(syntax.key);
                break;
            case 'RemoveViaJoinSyntax':
                if (syntax.key != null) this.visitExpression(syntax.key);
                break;
        }
    }

    visitJoinEvent(syntax: JoinEventSyntax): void {
        this.visitNode(syntax);
        syntax.mappings.forEach(node => this.visitMapping(node));
    }

    visitMapping(syntax: MappingSyntax): void {
        this.visitNode(syntax);
        if (syntax.source !== undefined) this.visitExpression(syntax.source);
        if (syntax.value !== undefined) this.visitExpression(syntax.value);
    }

    visitCondition(syntax: ConditionSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'ComparisonConditionSyntax') this.visitExpression(syntax.right);
        else { this.visitCondition(syntax.left); this.visitCondition(syntax.right); }
    }

    visitReaction(syntax: ReactionSyntax): void {
        this.visitNode(syntax);
        if (syntax.where != null) this.visitCondition(syntax.where);
        syntax.triggers.forEach(node => this.visitReactionTrigger(node));
    }

    visitReactionTrigger(syntax: ReactionTriggerSyntax): void {
        this.visitNode(syntax);
        this.visitTriggerSource(syntax.source);
        syntax.produces.forEach(node => this.visitProduces(node));
        syntax.invokes.forEach(node => this.visitInvokes(node));
    }

    visitProduces(syntax: ProducesSyntax): void {
        this.visitNode(syntax);
        if (syntax.when != null) this.visitCondition(syntax.when);
        if (syntax.inlineEvent !== null) this.visitEvent(syntax.inlineEvent);
        if (syntax.inlineOperation != null) this.visitOperation(syntax.inlineOperation);
        if (syntax.for !== null) this.visitExpression(syntax.for);
        syntax.mappings.forEach(node => this.visitPropertyMapping(node));
        syntax.tags.forEach(node => this.visitTag(node));
    }

    visitInvokes(syntax: InvokesSyntax): void {
        this.visitNode(syntax);
        syntax.mappings?.forEach(node => this.visitPropertyMapping(node));
        syntax.onRefused?.forEach(node => this.visitInvocationRefusal(node));
    }

    visitInvocationRefusal(syntax: InvocationRefusalSyntax): void {
        this.visitNode(syntax);
        syntax.produces.forEach(node => this.visitProduces(node));
    }

    visitCapture(syntax: CaptureSyntax): void {
        this.visitNode(syntax);
        if (syntax.source !== null) this.visitCaptureSource(syntax.source);
        syntax.map?.forEach(node => this.visitCaptureMap(node));
        syntax.appends.forEach(node => this.visitCaptureAppend(node));
        syntax.children.forEach(node => this.visitCaptureChildren(node));
        syntax.nested.forEach(node => this.visitCaptureNested(node));
    }

    visitCaptureSource(syntax: CaptureSourceSyntax): void {
        this.visitNode(syntax);
        syntax.settings.forEach(node => this.visitCaptureSourceSetting(node));
    }

    visitCaptureSourceSetting(syntax: CaptureSourceSettingSyntax): void {
        this.visitNode(syntax);
    }

    visitCaptureMap(syntax: CaptureMapOperationSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.source);
        if (syntax.kind === 'CaptureMapEntrySyntax') syntax.translations.forEach(node => this.visitNode(node));
    }

    visitCaptureAppend(syntax: CaptureAppendSyntax): void {
        this.visitNode(syntax);
        if (syntax.when != null) this.visitNode(syntax.when);
        syntax.mappings?.forEach(node => this.visitPropertyMapping(node));
        syntax.tags?.forEach(node => this.visitTag(node));
    }

    visitCaptureChildren(syntax: CaptureChildrenSyntax): void {
        this.visitNode(syntax);
        syntax.map?.forEach(node => this.visitCaptureMap(node));
        syntax.appends.forEach(node => this.visitCaptureAppend(node));
    }

    visitCaptureNested(syntax: CaptureNestedSyntax): void {
        this.visitNode(syntax);
        syntax.map?.forEach(node => this.visitCaptureMap(node));
        syntax.appends.forEach(node => this.visitCaptureAppend(node));
    }

    visitTriggerSource(syntax: TriggerSourceSyntax): void {
        this.visitNode(syntax);
    }

    visitScreen(syntax: ScreenSyntax): void {
        this.visitNode(syntax);
        syntax.directives.forEach(node => this.visitScreenDirective(node));
        syntax.contributions?.forEach(contribution => {
            this.visitNode(contribution);
            contribution.directives.forEach(node => this.visitScreenDirective(node));
        });
    }

    visitScreenDirective(syntax: ScreenDirectiveSyntax): void {
        if (syntax.kind === 'ScreenGuardedActionSyntax') {
            this.visitScreenGuardedAction(syntax);
            return;
        }
        this.visitNode(syntax);
        switch (syntax.kind) {
            case 'ScreenDataSyntax':
                this.visitTypeRef(syntax.type);
                break;
            case 'ScreenActionSyntax':
                if (syntax.navigate !== null) this.visitScreenDirective(syntax.navigate);
                break;
            case 'ScreenNavigateSyntax':
                syntax.parameters.forEach(parameter => {
                    this.visitNode(parameter);
                    this.visitUiBinding(parameter.binding);
                });
                break;
            case 'ScreenTemplateReferenceSyntax':
                syntax.slots.forEach(slot => {
                    this.visitNode(slot);
                    slot.directives.forEach(node => this.visitScreenDirective(node));
                });
                break;
            case 'ScreenSectionSyntax':
                syntax.directives.forEach(node => this.visitScreenDirective(node));
                break;
            case 'ScreenTableSyntax':
                syntax.columns.forEach(node => this.visitNode(node));
                if (syntax.rowClick !== null) this.visitScreenDirective(syntax.rowClick);
                break;
            case 'ScreenSummarySyntax':
                syntax.fields.forEach(node => this.visitNode(node));
                break;
            case 'ScreenComponentSyntax':
                if (syntax.context !== null) this.visitUiBinding(syntax.context);
                syntax.properties.forEach(property => {
                    this.visitNode(property);
                    if (property.binding !== null) this.visitUiBinding(property.binding);
                    if (property.value !== null) this.visitExpression(property.value);
                });
                syntax.exposes.forEach(exposed => {
                    this.visitNode(exposed);
                    this.visitUiBinding(exposed.binding);
                });
                syntax.presentation.forEach(node => this.visitNode(node));
                syntax.outlets.forEach(outlet => {
                    this.visitNode(outlet);
                    outlet.directives.forEach(node => this.visitScreenDirective(node));
                });
                break;
            case 'ScreenToolbarSyntax':
                syntax.items.forEach(item => {
                    this.visitNode(item);
                    item.parameters.forEach(parameter => {
                        this.visitNode(parameter);
                        this.visitUiBinding(parameter.binding);
                    });
                    item.presentation.forEach(node => this.visitNode(node));
                });
                break;
            case 'ScreenCodeSyntax':
                this.visitNode(syntax.code);
                break;
        }
    }

    visitScreenGuardedAction(syntax: ScreenGuardedActionSyntax): void {
        this.visitNode(syntax);
        syntax.alternatives.forEach(node => this.visitScreenActionAlternative(node));
        if (syntax.otherwise !== null) this.visitScreenActionOtherwise(syntax.otherwise);
        if (syntax.navigate !== null) this.visitScreenDirective(syntax.navigate);
    }

    visitScreenActionAlternative(syntax: ScreenActionAlternativeSyntax): void {
        this.visitNode(syntax);
        this.visitCondition(syntax.condition);
        syntax.arguments.forEach(node => this.visitInteractionArgument(node));
    }

    visitScreenActionOtherwise(syntax: ScreenActionOtherwiseSyntax): void {
        this.visitNode(syntax);
        syntax.arguments.forEach(node => this.visitInteractionArgument(node));
    }

    visitInteractionArgument(syntax: InteractionArgumentSyntax): void { this.visitNode(syntax); }

    visitSpecification(syntax: SpecificationSyntax): void {
        this.visitNode(syntax);
        syntax.examples?.forEach(node => this.visitSpecificationExample(node));
        syntax.parameters?.forEach(node => { this.visitNode(node); this.visitTypeRef(node.type); });
        syntax.cases?.forEach(node => { this.visitNode(node); node.values.forEach(value => this.visitPropertyMapping(value)); });
        if (syntax.givenClock !== null) this.visitSpecificationClock(syntax.givenClock);
        syntax.givenOperationFailures?.forEach(node => this.visitSpecificationOperationFailure(node));
        syntax.thenOperations?.forEach(node => this.visitSpecificationOperation(node));
        syntax.thenCompensated?.forEach(node => this.visitSpecificationCompensated(node));
        if (syntax.givenCallerPersona != null) this.visitNode(syntax.givenCallerPersona);
        if (syntax.givenCaller != null) {
            this.visitNode(syntax.givenCaller);
            syntax.givenCaller.claims.forEach(node => this.visitNode(node));
        }
        syntax.given.forEach(node => this.visitSpecificationEvent(node));
        syntax.givenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        if (syntax.when !== null) this.visitSpecificationCommand(syntax.when);
        syntax.givenCaptures.forEach(node => this.visitSpecificationCapture(node));
        if (syntax.whenAppended !== null) this.visitSpecificationEvent(syntax.whenAppended);
        if (syntax.whenRedelivered != null) this.visitSpecificationRedelivery(syntax.whenRedelivered);
        if (syntax.whenClock !== null) this.visitSpecificationClock(syntax.whenClock);
        if (syntax.whenTrigger !== null) this.visitSpecificationTrigger(syntax.whenTrigger);
        if (syntax.whenCapture !== null) this.visitSpecificationCapture(syntax.whenCapture);
        if (syntax.whenQuery !== null) this.visitSpecificationWhenQuery(syntax.whenQuery);
        syntax.thenEvents.forEach(node => this.visitSpecificationEvent(node));
        syntax.thenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        syntax.thenAbsentReadModels?.forEach(node => { this.visitNode(node); this.visitExpression(node.key); });
        syntax.thenQueries?.forEach(node => {
            this.visitNode(node);
            node.arguments.forEach(value => this.visitPropertyMapping(value));
            node.results.forEach(result => this.visitSpecificationQueryResult(result));
        });
        syntax.thenResults.forEach(node => this.visitSpecificationQueryResult(node));
        if (syntax.thenNoResult !== null) this.visitNode(syntax.thenNoResult);
        if (syntax.thenDenied != null) this.visitNode(syntax.thenDenied);
        if (syntax.thenReturns != null) this.visitSpecificationReturn(syntax.thenReturns);
        syntax.thenErrors.forEach(node => { this.visitNode(node); if (node.caseValue != null) this.visitExpression(node.caseValue); });
    }

    visitSpecificationRedelivery(syntax: SpecificationRedeliverySyntax): void {
        this.visitNode(syntax);
        if (syntax.for !== null) this.visitExpression(syntax.for);
        if (syntax.stream != null) this.visitSpecificationStream(syntax.stream);
        if (syntax.noStream != null) this.visitSpecificationNoStream(syntax.noStream);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationClock(syntax: SpecificationClockSyntax): void {
        this.visitNode(syntax);
    }

    visitSpecificationTrigger(syntax: SpecificationTriggerSyntax): void {
        this.visitNode(syntax);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationCapture(syntax: SpecificationCaptureSyntax): void {
        this.visitNode(syntax);
        syntax.record.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationWhenQuery(syntax: SpecificationWhenQuerySyntax): void {
        this.visitNode(syntax);
        syntax.arguments.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationQueryResult(syntax: SpecificationQueryResultSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationExample(syntax: SpecificationExampleSyntax): void {
        this.visitNode(syntax);
        if (syntax.for !== null) this.visitExpression(syntax.for);
        if (syntax.stream != null) this.visitSpecificationStream(syntax.stream);
        if (syntax.noStream != null) this.visitSpecificationNoStream(syntax.noStream);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
        syntax.generatedValues.forEach(node => this.visitPropertyMapping(node));
    }

    visitSpecificationEvent(syntax: SpecificationEventSyntax): void {
        this.visitNode(syntax);
        if (syntax.stream != null) this.visitSpecificationStream(syntax.stream);
        if (syntax.noStream != null) this.visitSpecificationNoStream(syntax.noStream);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
        if (syntax.for !== null) this.visitExpression(syntax.for);
    }

    visitSpecificationStream(syntax: SpecificationStreamSyntax): void {
        this.visitNode(syntax);
        if (syntax.streamId !== null) this.visitPropertyMapping(syntax.streamId);
        syntax.streamIdParts.forEach(part => this.visitPropertyMapping(part));
    }

    visitSpecificationNoStream(syntax: SpecificationNoStreamSyntax): void { this.visitNode(syntax); }

    visitSpecificationCommand(syntax: SpecificationCommandSyntax): void {
        this.visitNode(syntax);
        (syntax.generatedValues ?? []).forEach(fixture => this.visitPropertyMapping(fixture));
        syntax.values.forEach(node => this.visitPropertyMapping(node));
        if (syntax.for !== null) this.visitExpression(syntax.for);
    }

    visitSpecificationReadModel(syntax: SpecificationReadModelSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitPropertyMapping(node));
    }

    visitPropertyMapping(syntax: PropertyMappingSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.source);
    }

    visitProperty(syntax: PropertySyntax): void {
        this.visitNode(syntax);
        this.visitTypeRef(syntax.type);
    }

    visitTypeRef(syntax: TypeRefSyntax): void {
        this.visitNode(syntax);
    }

    visitUiBinding(syntax: import('./Screens').UiBindingSyntax): void {
        this.visitNode(syntax);
        if (syntax.literal !== null) this.visitExpression(syntax.literal);
    }

    visitIdentityExpression(syntax: IdentityExpressionSyntax): void { this.visitNode(syntax); }

    visitExpression(syntax: ExpressionSyntax): void {
        if (syntax.kind === 'IdentityExpressionSyntax') { this.visitIdentityExpression(syntax); return; }
        this.visitNode(syntax);
        if (syntax.kind === 'ListExpressionSyntax') {
            syntax.items.forEach(item => this.visitExpression(item));
        } else if (syntax.kind === 'ObjectExpressionSyntax') {
            syntax.members.forEach(member => this.visitObjectMember(member));
        } else if (syntax.kind === 'TemplateExpressionSyntax') {
            syntax.parts.forEach(part => { this.visitNode(part); if (part.kind === 'TemplateInterpolationSyntax') this.visitExpression(part.expression); });
        }
    }

    visitObjectMember(syntax: ObjectMemberSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.value);
    }
}
