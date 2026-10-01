// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CaptureAppendSyntax, CaptureChildrenSyntax, CaptureNestedSyntax, CaptureSourceSettingSyntax, CaptureSourceSyntax, CaptureSyntax } from './Captures';
import { CommandSyntax, ValidateSyntax, ValidationRuleSyntax } from './Commands';
import { ConstraintSyntax } from './Constraints';
import { ConceptSyntax, DomainSyntax, EventSyntax, ImportSyntax, PropertySyntax, ReadModelSyntax, TagSyntax, TypeRefSyntax, TypeSyntax } from './Declarations';
import { ExpressionSyntax, ObjectMemberSyntax, PropertyMappingSyntax } from './Expressions';
import { JoinEventSyntax, MappingSyntax, ProjectionBlockSyntax, ProjectionSyntax } from './Projections';
import { QueryParameterSyntax, QuerySyntax } from './Queries';
import { InvokesSyntax, ProducesSyntax, ReactionSyntax, ReactionTriggerSyntax, TriggerSourceSyntax } from './Reactions';
import { ScreenDirectiveSyntax, ScreenSyntax } from './Screens';
import { SpecificationCommandSyntax, SpecificationEventSyntax, SpecificationReadModelSyntax, SpecificationSyntax } from './Specifications';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from './Structure';
import { SyntaxNode } from './SyntaxNode';

// Walks a whole syntax tree, depth first, in the order the C# ScreenplaySyntaxWalker does. Every visit
// method calls visitNode and then walks the node's children, so an emitter overrides only the nodes it
// cares about - and calls the base method when it still wants the children walked.
export abstract class ScreenplaySyntaxWalker {
    visitNode(_node: SyntaxNode): void {}

    visitApplication(syntax: ApplicationSyntax): void {
        this.visitNode(syntax);
        if (syntax.domain !== null) this.visitDomain(syntax.domain);
        syntax.imports.forEach(node => this.visitImport(node));
        syntax.concepts.forEach(node => this.visitConcept(node));
        syntax.types.forEach(node => this.visitType(node));
        syntax.modules.forEach(node => this.visitModule(node));
    }

    visitDomain(syntax: DomainSyntax): void {
        this.visitNode(syntax);
    }

    visitImport(syntax: ImportSyntax): void {
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
        syntax.features.forEach(node => this.visitFeature(node));
    }

    visitFeature(syntax: FeatureSyntax): void {
        this.visitNode(syntax);
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
        syntax.validations.forEach(node => this.visitValidate(node));
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
        syntax.given.forEach(node => this.visitSpecificationEvent(node));
        syntax.givenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        if (syntax.when !== null) this.visitSpecificationCommand(syntax.when);
        if (syntax.whenAppended !== null) this.visitSpecificationEvent(syntax.whenAppended);
        syntax.thenEvents.forEach(node => this.visitSpecificationEvent(node));
        syntax.thenReadModels.forEach(node => this.visitSpecificationReadModel(node));
        syntax.thenErrors.forEach(node => this.visitNode(node));
    }

    visitSpecificationEvent(syntax: SpecificationEventSyntax): void {
        this.visitNode(syntax);
        syntax.values.forEach(node => this.visitPropertyMapping(node));
        if (syntax.for !== null) this.visitExpression(syntax.for);
    }

    visitSpecificationCommand(syntax: SpecificationCommandSyntax): void {
        this.visitNode(syntax);
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
