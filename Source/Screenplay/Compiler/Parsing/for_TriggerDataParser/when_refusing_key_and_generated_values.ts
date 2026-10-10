// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parseForAuthoring } from '../../ScreenplayCompiler';

describe('when refusing key and generated trigger values', () => {
    it.each([
        ['id String key', DiagnosticCodes.InvalidReadModelKey],
        ['id Token generated', DiagnosticCodes.GeneratedPropertyOutsideCommand],
    ])('should refuse %s while retaining typed trigger evidence', (property, code) => {
        const result = parseForAuthoring(`concept Token : Uuid\ntrigger Arrived\n  ${property}\n  kept String`);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
        expect(result.triggerData.map(property => property.name)).toEqual(['id', 'kept']);
    });
    it('should refuse malformed optional type spelling without consuming the next value', () => {
        const result = parseForAuthoring('trigger Arrived\n  id String optional optional\n    ignored String\n  kept String');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.InvalidTriggerData);
        expect(result.triggerData.map(property => property.name)).toEqual(['kept']);
    });
});
