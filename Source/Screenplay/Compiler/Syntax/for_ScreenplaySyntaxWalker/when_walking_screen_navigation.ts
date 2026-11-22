// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenNavigateSyntax } from '../Screens';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { binding, location, navigation } from './given/syntax';

describe('when walking screen navigation', given(a_recording_walker, context => {
    const first = { kind: 'ScreenNavigationParameterSyntax' as const, name: 'id', binding: binding('id'), location };
    const second = { kind: 'ScreenNavigationParameterSyntax' as const, name: 'tab', binding: binding('tab'), location };
    const syntax: ScreenNavigateSyntax = { ...navigation('Details'), parameters: [first, second] };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should dispatch each parameter binding immediately after its node', () => {
        context.hooks.should.deep.equal(['directive', 'node', 'node', 'binding', 'node', 'node', 'binding', 'node']);
    });

    it('should preserve parameter and binding identity and sibling order', () => {
        context.nodes.should.have.ordered.members([syntax, syntax, first, first.binding, first.binding, second, second.binding, second.binding]);
    });
}));
