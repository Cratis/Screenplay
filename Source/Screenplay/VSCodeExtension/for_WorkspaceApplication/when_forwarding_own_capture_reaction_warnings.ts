// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

const source = [
    'module M',
    '  feature F',
    '    slice Translate Import',
    '      event Recorded',
    '      capture Feed',
    '        append Recorded',
    '      reaction FollowUp',
    '        when Recorded',
].join('\n');

describe('when forwarding own capture reaction warnings in workspace files', () => {
    it('surfaces the warning at the reaction', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', source);
        const warnings = application.diagnosticsFor('application.play').filter(item => item.code === 'PLAY0694');
        expect(warnings.map(item => [item.severity, item.location.line])).toEqual([['warning', 7]]);
    });
});
