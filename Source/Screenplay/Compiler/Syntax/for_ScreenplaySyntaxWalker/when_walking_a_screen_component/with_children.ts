// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ComponentPropertySyntax, ComponentSlotSyntax, ScreenComponentSyntax } from '../../Screens';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { binding, component, literal, location, title } from '../given/syntax';

describe('when walking a screen component with children', given(a_recording_walker, context => {
    const dataContext = binding('card');
    const first: ComponentPropertySyntax = { kind: 'ComponentPropertySyntax', property: 'label', binding: binding('label'), value: literal('Label'), location };
    const second: ComponentPropertySyntax = { kind: 'ComponentPropertySyntax', property: 'hint', binding: binding('hint'), value: literal('Hint'), location };
    const exposed = { kind: 'ComponentExposedValueSyntax' as const, name: 'selected', binding: binding('selection'), location };
    const anotherExposed = { kind: 'ComponentExposedValueSyntax' as const, name: 'count', binding: binding('count'), location };
    const presentation = { kind: 'PresentationValueSyntax' as const, name: 'color', value: 'blue', location };
    const anotherPresentation = { kind: 'PresentationValueSyntax' as const, name: 'size', value: 'large', location };
    const heading = title('Heading');
    const footer = title('Footer');
    const slot: ComponentSlotSyntax = { kind: 'ComponentSlotSyntax', name: 'body', directives: [heading], location };
    const anotherSlot: ComponentSlotSyntax = { kind: 'ComponentSlotSyntax', name: 'footer', directives: [footer], location };
    const syntax: ScreenComponentSyntax = { ...component(), context: dataContext, properties: [first, second],
        exposes: [exposed, anotherExposed], presentation: [presentation, anotherPresentation], slots: [slot, anotherSlot] };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should dispatch both the binding and value of each property through public hooks', () => {
        context.hooks.should.deep.equal([
            'directive', 'node', 'binding', 'node',
            'node', 'binding', 'node', 'expression', 'node',
            'node', 'binding', 'node', 'expression', 'node',
            'node', 'binding', 'node', 'node', 'binding', 'node', 'node', 'node',
            'node', 'directive', 'node', 'node', 'directive', 'node',
        ]);
    });

    it('should preserve context property expose presentation and slot order and identity', () => {
        context.nodes.should.have.ordered.members([
            syntax, syntax, dataContext, dataContext,
            first, first.binding, first.binding, first.value, first.value,
            second, second.binding, second.binding, second.value, second.value,
            exposed, exposed.binding, exposed.binding, anotherExposed, anotherExposed.binding, anotherExposed.binding,
            presentation, anotherPresentation, slot, heading, heading, anotherSlot, footer, footer,
        ]);
    });
}));
