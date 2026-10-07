// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const cases = [
    { code: 'PLAY0514', source: ['module M', '  feature F', '    slice StateView S', '      event E', '      readmodel V', '        value String', '      projection P => V', '        from E', '          missing = "recorded"'] },
    { code: 'PLAY0515', source: ['concept PatientId : Uuid @pii', 'trigger External', '  patient PatientId', 'module M', '  feature F', '    slice Automation S', '      event E', '      reaction R', '        when External', '          produces E', '            for patient'] },
    { code: 'PLAY0166', source: ['module M', '  feature F', '    slice StateView S', '      projection P', '        remove with Missing'] },
    { code: 'PLAY0166', source: ['module M', '  feature F', '    slice Translate S', '      capture C', '        source api', '          api LegacyApi', '        key id', '        append Missing', '          when added'] },
];

describe('when surfacing compiler consistency diagnostics', () => {
    it.each(cases)('should surface $code once for $source', ({ code, source }) => {
        expect(validateLines(source).filter(issue => issue.code === code)).toHaveLength(1);
    });

    it('should not duplicate an event diagnostic supplied by the compiler', () => {
        const source = ['module M', '  feature F', '    slice StateChange S', '      command C', '        produces Missing', '          for "id"'];
        const diagnostics = [{ code: 'PLAY0166', severity: 'warning' as const, message: 'Unknown event', location: { line: 5, column: 9 } }];
        expect(validateLines(source, { compilerDiagnostics: diagnostics }).filter(issue => issue.code === 'PLAY0166')).toHaveLength(1);
    });
});
