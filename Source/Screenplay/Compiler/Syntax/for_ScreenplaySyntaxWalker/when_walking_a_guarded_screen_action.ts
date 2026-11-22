// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenActionAlternativeSyntax, ScreenActionOtherwiseSyntax, ScreenGuardedActionSyntax } from '../Screens';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { condition, location, navigation } from './given/syntax';

describe('when walking a guarded screen action', given(a_recording_walker, context => {
    const enabled = condition('ready');
    const alternative: ScreenActionAlternativeSyntax = { kind: 'ScreenActionAlternativeSyntax', condition: enabled, command: 'Submit', arguments: [], location };
    const otherwise: ScreenActionOtherwiseSyntax = { kind: 'ScreenActionOtherwiseSyntax', outcome: 'Execute', command: 'Save', arguments: [], location };
    const navigate = navigation('Details');
    const syntax: ScreenGuardedActionSyntax = { kind: 'ScreenGuardedActionSyntax', label: 'Submit', alternatives: [alternative], otherwise, navigate, location };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should dispatch the guarded action before its single node visit', () => {
        context.hooks.should.deep.equal([
            'directive', 'guarded action', 'node', 'node', 'condition', 'node', 'expression', 'node', 'node', 'directive', 'node',
        ]);
    });

    it('should let the guarded action hook own its ordered descendants', () => {
        context.nodes.should.have.ordered.members([
            syntax, syntax, syntax, alternative, enabled, enabled, enabled.right, enabled.right, otherwise, navigate, navigate,
        ]);
    });
}));
