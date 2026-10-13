// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

const source = ['module M', '  feature F', '    slice StateChange S', '      event Renamed', '        id "Original"'];
const message = "Event 'Renamed' id pin 'Original' is not yet carried to renderers; generated code uses the new name 'Renamed'.";

describe('when forwarding event pin warnings', () => {
    it('preserves supplied C# binder warnings with their code, message and location', () => {
        expect(DiagnosticCodes.EventIdPinNotCarriedToRenderers).toBe(diagnosticCodes.eventIdPinNotCarriedToRenderers);
        const diagnostics = [{ code: DiagnosticCodes.EventIdPinNotCarriedToRenderers, severity: 'warning' as const, message, location: { line: 4, column: 7 } }];
        expect(validateLines(source, { compilerDiagnostics: diagnostics }).filter(issue => issue.code === 'PLAY0686')).toEqual([
            expect.objectContaining({ code: 'PLAY0686', severity: 'warning', message, line: 3, startColumn: 7 }),
        ]);
    });

    it('does not claim ordinary TypeScript validation runs the semantic binder', () => {
        expect(validateLines(source).filter(issue => issue.code === 'PLAY0686')).toEqual([]);
    });
});
