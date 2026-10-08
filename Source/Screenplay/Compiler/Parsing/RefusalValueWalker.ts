// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { ExpressionSyntax, ObjectMemberSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { InvocationRefusalSyntax } from '../Syntax/InvocationRefusalSyntax';
import { ProducesSyntax } from '../Syntax/Reactions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';

export class RefusalValueWalker extends ScreenplaySyntaxWalker {
    private slice: SliceSyntax | null = null;
    private branch: InvocationRefusalSyntax | null = null;
    private properties: readonly PropertySyntax[] | null = null;
    private target: TypeRefSyntax | null = null;
    private mapping = false;

    constructor(private readonly declarations: RefusalDeclarations, private readonly context: ParserContext) { super(); }

    visitSlice(syntax: SliceSyntax): void {
        this.slice = syntax;
        super.visitSlice(syntax);
        this.slice = null;
    }

    visitInvocationRefusal(syntax: InvocationRefusalSyntax): void {
        this.branch = syntax;
        super.visitInvocationRefusal(syntax);
        this.branch = null;
    }

    visitProduces(syntax: ProducesSyntax): void {
        if (this.branch === null || this.slice === null) { super.visitProduces(syntax); return; }
        this.visitNode(syntax);
        if (syntax.for !== null) this.visitExpression(syntax.for);
        if (syntax.when != null) this.visitCondition(syntax.when);
        syntax.tags.forEach(tag => this.visitTag(tag));
        this.properties = this.declarations.event(syntax.event, this.slice)?.properties ?? null;
        syntax.mappings.forEach(mapping => this.visitPropertyMapping(mapping));
        this.properties = null;
    }

    visitPropertyMapping(syntax: PropertyMappingSyntax): void {
        this.mapping = this.branch !== null;
        this.target = this.declarations.property(this.properties, syntax.property)?.type ?? null;
        super.visitPropertyMapping(syntax);
        this.target = null;
        this.mapping = false;
    }

    visitObjectMember(syntax: ObjectMemberSyntax): void {
        const parent = this.target;
        this.target = this.declarations.property(parent === null ? null : this.declarations.types.get(parent.name)?.properties ?? null, syntax.name)?.type ?? null;
        super.visitObjectMember(syntax);
        this.target = parent;
    }

    visitExpression(syntax: ExpressionSyntax): void {
        if (syntax.kind === 'RefusalExpressionSyntax') {
            if (!this.mapping || this.branch === null || !['reason', 'constraint', 'message'].includes(syntax.member) || syntax.member === 'constraint' && this.branch.selector !== 'constraint') {
                this.context.error(DiagnosticCodes.InvalidRefusalValue, `'$refusal.${syntax.member}' is only available in a refusal branch's event mapping; constraint requires 'by constraint'.`, syntax.location);
            } else if (this.target !== null && (this.target.isCollection || this.declarations.compatible({ ...this.target, name: 'String', isOptional: false, isCollection: false }, this.target) === false && !this.declarations.application.concepts.some(concept => concept.name === this.target!.name && concept.type === 'String'))) {
                this.context.error(DiagnosticCodes.InvalidRefusalValue, `'$refusal.${syntax.member}' is a String value, incompatible with '${this.target.name}'.`, syntax.location);
            }
        }
        const parent = this.target;
        if (syntax.kind === 'ListExpressionSyntax' && parent !== null) this.target = { ...parent, isCollection: false };
        super.visitExpression(syntax);
        this.target = parent;
    }
}
