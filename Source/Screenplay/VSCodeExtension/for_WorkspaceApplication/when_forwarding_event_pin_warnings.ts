// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as compiler from '@cratis/screenplay-compiler';
import { WorkspaceApplication } from '../WorkspaceApplication';

const source = 'module M\n  feature F\n    slice StateChange S\n      event Renamed\n        id "Original"';

describe('when forwarding event pin warnings in workspace files', () => {
    it('retains a supplied binder warning without changing its severity, message or location', () => {
        const diagnostic = {
            code: compiler.DiagnosticCodes.EventIdPinNotCarriedToRenderers,
            severity: 'warning' as const,
            message: "Event 'Renamed' id pin 'Original' is not yet carried to renderers; generated code uses the new name 'Renamed'.",
            location: { path: 'application.play', line: 4, column: 7 },
        };
        const compiled = compiler.compileApplication(new Map([['application.play', source]]));
        const supplied = vi.spyOn(compiler, 'compileApplication').mockReturnValueOnce({ ...compiled, diagnostics: [...compiled.diagnostics, diagnostic] });
        try {
            const application = new WorkspaceApplication();
            application.set('application.play', source);
            expect(application.diagnosticsFor('application.play').filter(item => item.code === 'PLAY0686')).toEqual([diagnostic]);
        } finally {
            supplied.mockRestore();
        }
    });

    it('does not claim workspace TypeScript compilation runs the semantic binder', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', source);
        expect(application.diagnosticsFor('application.play').filter(item => item.code === 'PLAY0686')).toEqual([]);
    });
});
