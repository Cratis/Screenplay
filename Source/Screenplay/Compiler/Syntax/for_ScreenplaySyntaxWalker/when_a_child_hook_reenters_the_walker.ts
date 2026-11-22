// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ComponentPropertySyntax, ScreenComponentSyntax, UiBindingSyntax } from '../Screens';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { binding, component, literal, location, navigation } from './given/syntax';

class a_reentrant_walker extends a_recording_walker {
    readonly trigger = binding('outer');
    readonly inner = navigation('Nested');

    override visitUiBinding(syntax: UiBindingSyntax): void {
        super.visitUiBinding(syntax);
        if (syntax === this.trigger) this.visitScreenDirective(this.inner);
    }
}

describe('when a child hook reenters the walker', given(a_reentrant_walker, context => {
    const first: ComponentPropertySyntax = { kind: 'ComponentPropertySyntax', property: 'first', binding: context.trigger, value: literal('first'), location };
    const second: ComponentPropertySyntax = { kind: 'ComponentPropertySyntax', property: 'second', binding: binding('second'), value: literal('second'), location };
    const syntax: ScreenComponentSyntax = { ...component(), properties: [first, second] };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should finish the nested traversal before resuming the outer property', () => {
        context.hooks.should.deep.equal([
            'directive', 'node', 'node', 'binding', 'node', 'directive', 'node', 'expression', 'node',
            'node', 'binding', 'node', 'expression', 'node',
        ]);
    });

    it('should resume the original subtree without losing identity or the next sibling', () => {
        context.nodes.should.have.ordered.members([
            syntax, syntax, first, context.trigger, context.trigger, context.inner, context.inner, first.value, first.value,
            second, second.binding, second.binding, second.value, second.value,
        ]);
    });
}));
