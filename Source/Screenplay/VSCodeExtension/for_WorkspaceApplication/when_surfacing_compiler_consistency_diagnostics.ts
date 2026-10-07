// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';

const cases = [
    { code: 'PLAY0514', source: ['slice StateView S', '  event E', '  readmodel V', '    value String', '  projection P => V', '    from E', '      missing = "recorded"'] },
    { code: 'PLAY0515', source: ['slice Automation S', '  event E', '  reaction R', '    when External', '      produces E', '        for patient'] },
    { code: 'PLAY0166', source: ['slice StateView S', '  projection P', '    remove with Missing'] },
    { code: 'PLAY0166', source: ['slice Translate S', '  capture C', '    source api', '      api LegacyApi', '    key id', '    append Missing', '      when added'] },
];

describe('when surfacing compiler consistency diagnostics across files', () => {
    it.each(cases)('should surface $code once in an imported fragment', ({ code, source }) => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'import "types.play"\nmodule M\n  feature F\n    import "slice.play"');
        application.set('types.play', 'concept PatientId : Uuid @pii\ntrigger External\n  patient PatientId');
        application.set('slice.play', source.join('\n'));
        const diagnostics = application.diagnosticsFor('slice.play');
        expect(diagnostics.filter(diagnostic => diagnostic.code === code)).toHaveLength(1);
        expect(validateLines(source, { application: application.symbolsExcept('slice.play'), placement: application.placementOf('slice.play'), path: 'slice.play', compilerDiagnostics: diagnostics }).filter(issue => issue.code === code)).toHaveLength(1);
    });
});
