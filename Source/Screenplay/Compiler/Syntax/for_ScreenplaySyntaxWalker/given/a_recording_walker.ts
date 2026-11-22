// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach } from 'vitest';
import { ScreenplaySyntaxWalker } from '../../ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../SyntaxNode';
import { ExpressionSyntax, PropertyMappingSyntax } from '../../Expressions';
import { ConditionSyntax } from '../../Conditions';
import { ScreenDirectiveSyntax, ScreenGuardedActionSyntax, UiBindingSyntax } from '../../Screens';

export class a_recording_walker extends ScreenplaySyntaxWalker {
    readonly hooks: string[] = [];
    readonly nodes: SyntaxNode[] = [];

    constructor() {
        super();
        beforeEach(() => {
            this.hooks.length = 0;
            this.nodes.length = 0;
        });
    }

    record(hook: string, node: SyntaxNode): void {
        this.hooks.push(hook);
        this.nodes.push(node);
    }

    override visitNode(node: SyntaxNode): void {
        this.record('node', node);
    }

    override visitScreenDirective(syntax: ScreenDirectiveSyntax): void {
        this.record('directive', syntax);
        super.visitScreenDirective(syntax);
    }

    override visitScreenGuardedAction(syntax: ScreenGuardedActionSyntax): void {
        this.record('guarded action', syntax);
        super.visitScreenGuardedAction(syntax);
    }

    override visitUiBinding(syntax: UiBindingSyntax): void {
        this.record('binding', syntax);
        super.visitUiBinding(syntax);
    }

    override visitExpression(syntax: ExpressionSyntax): void {
        this.record('expression', syntax);
        super.visitExpression(syntax);
    }

    override visitPropertyMapping(syntax: PropertyMappingSyntax): void {
        this.record('mapping', syntax);
        super.visitPropertyMapping(syntax);
    }

    override visitCondition(syntax: ConditionSyntax): void {
        this.record('condition', syntax);
        super.visitCondition(syntax);
    }
}
