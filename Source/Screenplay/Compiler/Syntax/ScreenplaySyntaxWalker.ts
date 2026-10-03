// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax, PersonaSyntax, PolicyRequirementSyntax } from './Authorization';
import { CaptureAppendSyntax, CaptureChildrenSyntax, CaptureNestedSyntax, CaptureSourceSettingSyntax, CaptureSourceSyntax, CaptureSyntax } from './Captures';
import { CommandSyntax, ValidateSyntax, ValidationRuleSyntax } from './Commands';
import { ConstraintSyntax } from './Constraints';
import { ConceptSyntax, DomainSyntax, EventSyntax, ImportSyntax, PropertySyntax, ReadModelSyntax, TagSyntax, TypeRefSyntax, TypeSyntax } from './Declarations';
import { ExpressionSyntax, ObjectMemberSyntax, PropertyMappingSyntax } from './Expressions';
import { JoinEventSyntax, MappingSyntax, ProjectionBlockSyntax, ProjectionSyntax } from './Projections';
import { QueryParameterSyntax, QuerySyntax } from './Queries';
import { InvokesSyntax, ProducesSyntax, ReactionSyntax, ReactionTriggerSyntax, TriggerSourceSyntax } from './Reactions';
import { ScreenDirectiveSyntax, ScreenSyntax } from './Screens';
import {
    SpecificationCaptureSyntax, SpecificationClockSyntax, SpecificationCommandSyntax, SpecificationEventSyntax, SpecificationQueryResultSyntax,
    SpecificationReadModelSyntax, SpecificationSyntax, SpecificationTriggerSyntax, SpecificationWhenQuerySyntax,
} from './Specifications';
import { ApplicationSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax, SliceSyntax } from './Structure';
import { CommandResponseSyntax, PropertyResponseSourceSyntax, RecordCommandResponseSyntax, RecordSpecificationReturnSyntax, ResponseFieldSyntax, ScalarCommandResponseSyntax, ScalarSpecificationReturnSyntax, SpecificationReturnSyntax } from './Responses';
import { SyntaxNode } from './SyntaxNode';
import { CodeBlockSyntax, FileReferenceSyntax, HandlerSyntax, ImplementationSyntax, ImplementationHintSyntax } from './Implementations';

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
        syntax.concepts.forEach(node => this.visitConcept(node));
        syntax.types.forEach(node => this.visitType(node));
        syntax.personas.forEach(node => this.visitPersona(node));
        syntax.modules.forEach(node => this.visitModule(node));
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
    }

    visitType(syntax: TypeSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
    }

    visitModule(syntax: ModuleSyntax): void {
        this.visitNode(syntax);
        syntax.fileImports.forEach(node => this.visitFileImport(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        syntax.features.forEach(node => this.visitFeature(node));
    }

    visitFeature(syntax: FeatureSyntax): void {
        this.visitNode(syntax);
        syntax.fileImports.forEach(node => this.visitFileImport(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        syntax.features.forEach(node => this.visitFeature(node));
        syntax.slices.forEach(node => this.visitSlice(node));
    }

    visitSlice(syntax: SliceSyntax): void {
        this.visitNode(syntax);
        syntax.commands.forEach(node => this.visitCommand(node));
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

    visitCommand(syntax: CommandSyntax): void {
        this.visitNode(syntax);
        syntax.properties.forEach(node => this.visitProperty(node));
        if (syntax.authorize !== null) this.visitAuthorize(syntax.authorize);
        syntax.validations.forEach(node => this.visitValidate(node));
        syntax.produces.forEach(node => this.visitProduces(node));
        if (syntax.handler != null) this.visitHandler(syntax.handler);
        if (syntax.response != null) this.visitCommandResponse(syntax.response);
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
        if (syntax.kind === 'DeclarativeValidateSyntax') syntax.rules.forEach(node => this.visitValidationRule(node));
    }

    visitValidationRule(syntax: ValidationRuleSyntax): void {
        this.visitNode(syntax);
        if (syntax.value !== null) this.visitExpression(syntax.value);
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
    }

    visitProjection(syntax: ProjectionSyntax): void {
        this.visitNode(syntax);
        syntax.blocks.forEach(node => this.visitProjectionBlock(node));
    }

    visitProjectionBlock(syntax: ProjectionBlockSyntax): void {
        this.visitNode(syntax);
        switch (syntax.kind) {
            case 'FromSyntax':
                syntax.events.forEach(node => this.visitNode(node));
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
                syntax.entersOn.forEach(node => this.visitNode(node));
                syntax.blocks.forEach(node => this.visitProjectionBlock(node));
                break;
            case 'ChildrenSyntax':
            case 'NestedSyntax':
                syntax.blocks.forEach(node => this.visitProjectionBlock(node));
                break;
        }
    }

    visitJoinEvent(syntax: JoinEventSyntax): void {
        this.visitNode(syntax);
        syntax.mappings.forEach(node => this.visitMapping(node));
    }

    visitMapping(syntax: MappingSyntax): void {
        this.visitNode(syntax);
    }

    visitReaction(syntax: ReactionSyntax): void {
        this.visitNode(syntax);
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
        if (syntax.inlineEvent !== null) this.visitEvent(syntax.inlineEvent);
        if (syntax.for !== null) this.visitExpression(syntax.for);
        syntax.mappings.forEach(node => this.visitPropertyMapping(node));
        syntax.tags.forEach(node => this.visitTag(node));
    }

    visitInvokes(syntax: InvokesSyntax): void {
        this.visitNode(syntax);
    }

    visitCapture(syntax: CaptureSyntax): void {
        this.visitNode(syntax);
        if (syntax.source !== null) this.visitCaptureSource(syntax.source);
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

    visitCaptureAppend(syntax: CaptureAppendSyntax): void {
        this.visitNode(syntax);
    }

    visitCaptureChildren(syntax: CaptureChildrenSyntax): void {
        this.visitNode(syntax);
        syntax.appends.forEach(node => this.visitCaptureAppend(node));
    }

    visitCaptureNested(syntax: CaptureNestedSyntax): void {
        this.visitNode(syntax);
        syntax.appends.forEach(node => this.visitCaptureAppend(node));
    }

    visitTriggerSource(syntax: TriggerSourceSyntax): void {
        this.visitNode(syntax);
    }

    visitScreen(syntax: ScreenSyntax): void {
        this.visitNode(syntax);
        syntax.directives.forEach(node => this.visitScreenDirective(node));
    }

    visitScreenDirective(syntax: ScreenDirectiveSyntax): void {
        this.visitNode(syntax);
        switch (syntax.kind) {
            case 'ScreenDataSyntax':
                this.visitTypeRef(syntax.type);
                break;
            case 'ScreenActionSyntax':
                if (syntax.navigate !== null) this.visitScreenDirective(syntax.navigate);
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
            case 'ScreenCodeSyntax':
                this.visitNode(syntax.code);
                break;
        }
    }

    visitSpecification(syntax: SpecificationSyntax): void {
        this.visitNode(syntax);
        if (syntax.givenClock !== null) this.visitSpecificationClock(syntax.givenClock);
        syntax.given.forEach(node => this.visitSpecificationEvent(node));
        syntax.givenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        if (syntax.when !== null) this.visitSpecificationCommand(syntax.when);
        syntax.givenCaptures.forEach(node => this.visitSpecificationCapture(node));
        if (syntax.whenAppended !== null) this.visitSpecificationEvent(syntax.whenAppended);
        if (syntax.whenClock !== null) this.visitSpecificationClock(syntax.whenClock);
        if (syntax.whenTrigger !== null) this.visitSpecificationTrigger(syntax.whenTrigger);
        if (syntax.whenCapture !== null) this.visitSpecificationCapture(syntax.whenCapture);
        if (syntax.whenQuery !== null) this.visitSpecificationWhenQuery(syntax.whenQuery);
        syntax.thenEvents.forEach(node => this.visitSpecificationEvent(node));
        syntax.thenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        syntax.thenResults.forEach(node => this.visitSpecificationQueryResult(node));
        if (syntax.thenNoResult !== null) this.visitNode(syntax.thenNoResult);
        if (syntax.thenDenied != null) this.visitNode(syntax.thenDenied);
        if (syntax.thenReturns != null) this.visitSpecificationReturn(syntax.thenReturns);
        syntax.thenErrors.forEach(node => this.visitNode(node));
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

    visitSpecificationEvent(syntax: SpecificationEventSyntax): void {
        this.visitNode(syntax);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
        if (syntax.for !== null) this.visitExpression(syntax.for);
    }

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

    visitExpression(syntax: ExpressionSyntax): void {
        this.visitNode(syntax);
        if (syntax.kind === 'ListExpressionSyntax') {
            syntax.items.forEach(item => this.visitExpression(item));
        } else if (syntax.kind === 'ObjectExpressionSyntax') {
            syntax.members.forEach(member => this.visitObjectMember(member));
        }
    }

    visitObjectMember(syntax: ObjectMemberSyntax): void {
        this.visitNode(syntax);
        this.visitExpression(syntax.value);
    }
}
