// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenSectionSyntax, ScreenSlotSyntax, ScreenTemplateReferenceSyntax } from '../Screens';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { location, title } from './given/syntax';

describe('when walking a screen template reference', given(a_recording_walker, context => {
    const heading = title('Heading');
    const nested: ScreenSectionSyntax = { kind: 'ScreenSectionSyntax', name: 'content', directives: [heading], location };
    const footer = title('Footer');
    const first: ScreenSlotSyntax = { kind: 'ScreenSlotSyntax', name: 'body', directives: [nested], location };
    const second: ScreenSlotSyntax = { kind: 'ScreenSlotSyntax', name: 'footer', directives: [footer], location };
    const syntax: ScreenTemplateReferenceSyntax = { kind: 'ScreenTemplateReferenceSyntax', name: 'Page', slots: [first, second], location };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should dispatch nested directives through the public hook', () => {
        context.hooks.should.deep.equal(['directive', 'node', 'node', 'directive', 'node', 'directive', 'node', 'node', 'directive', 'node']);
    });

    it('should finish each slot subtree before visiting the next slot', () => {
        context.nodes.should.have.ordered.members([syntax, syntax, first, nested, nested, heading, heading, second, footer, footer]);
    });
}));
