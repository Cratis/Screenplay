// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenGuardedActionSyntax } from '../Screens';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { location, navigation } from './given/syntax';

class a_non_delegating_walker extends a_recording_walker {
    override visitScreenGuardedAction(syntax: ScreenGuardedActionSyntax): void {
        this.record('guarded action', syntax);
    }
}

describe('when a screen hook does not delegate', given(a_non_delegating_walker, context => {
    const syntax: ScreenGuardedActionSyntax = { kind: 'ScreenGuardedActionSyntax', label: 'Submit', alternatives: [],
        otherwise: null, navigate: navigation('Details'), location };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should not bypass the override to visit the node or its navigation', () => {
        context.hooks.should.deep.equal(['directive', 'guarded action']);
        context.nodes.should.have.ordered.members([syntax, syntax]);
    });
}));
