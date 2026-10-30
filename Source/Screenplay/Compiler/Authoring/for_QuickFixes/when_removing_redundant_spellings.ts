// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import * as compiler from '../../ScreenplayCompiler';
import { applyQuickFixEdits, findQuickFixes } from '../QuickFixes';

const head = 'module M\n  feature F\n    slice StateChange P\n      event E\n        id Uuid\n        other Uuid\n    slice StateView C\n      readmodel V\n        id Uuid\n      query FindC => V optional\n        by id Uuid\n      projection CProjection => V\n        from E key id\n';
const policies = 'policy Staff\n  require authenticated\npolicy Managers\n  require authenticated\npolicy Auditors\n  require authenticated\n';
const command = policies + 'module M\n  feature F\n    slice StateChange S\n      command C\n        id Uuid identifier\n';
const shape = (source: string) => JSON.stringify(compiler.parse(source).value, (key, value: unknown) => key === 'location' ? undefined : value);
const count = (source: string, code: string) => compiler.parse(source).diagnostics.filter(diagnostic => diagnostic.code === code).length;

describe('when removing redundant spellings', () => {
    it.each(['\n', '\r\n'])('should remove key braces, keep comments and prove the syntax unchanged with %j endings', ending => {
        const source = (head + '          key Composite { // keep\n            id = id // part\n            other = other\n          }\n          // trailing\n').replaceAll('\n', ending);
        const expected = (head + '          key Composite // keep\n            id = id // part\n            other = other\n          // trailing\n').replaceAll('\n', ending);
        const fixes = findQuickFixes(source, { line: 14, diagnosticCode: DiagnosticCodes.RedundantCompositeKeyBraces });
        expect(fixes).toHaveLength(1);
        const edited = applyQuickFixEdits(source, fixes[0].edits)!;
        expect(edited).toBe(expected);
        expect(shape(edited)).toBe(shape(source));
        expect(count(edited, DiagnosticCodes.RedundantCompositeKeyBraces)).toBe(0);
    });

    it('should remove a final closing brace without a newline', () => {
        const source = head + '          key {\n            id = id\n          }';
        const edited = applyQuickFixEdits(source, findQuickFixes(source, { line: 14 })[0].edits)!;
        expect(edited).toBe(head + '          key\n            id = id');
    });

    it('should refuse a closing brace that carries a comment', () => {
        expect(findQuickFixes(head + '          key {\n            id = id\n          } // keep\n', { line: 14 })).toEqual([]);
    });

    it.each([
        ['        authorize Staff Managers // both\n', '        authorize Staff and Managers // both\n'],
        ['        authorize (Staff or Managers) Auditors\n', '        authorize (Staff or Managers) and Auditors\n'],
        ['        authorize Staff and Managers\n          // between\n          Auditors // last\n', '        authorize Staff and Managers\n          // between\n          and Auditors // last\n'],
        ['        authorize Staff Managers\n          Auditors\n', '        authorize Staff and Managers\n          and Auditors\n'],
        ['        authorize Staff\r\n          Managers\r\n', '        authorize Staff\r\n          and Managers\r\n'],
    ])('should write the and in %j and prove the syntax unchanged', (source, expected) => {
        const fixes = findQuickFixes(command + source, { line: 12, diagnosticCode: DiagnosticCodes.ImplicitAuthorizationConjunction });
        expect(fixes).toHaveLength(1);
        const edited = applyQuickFixEdits(command + source, fixes[0].edits)!;
        expect(edited).toBe(command + expected);
        expect(shape(edited)).toBe(shape(command + source));
        expect(count(edited, DiagnosticCodes.ImplicitAuthorizationConjunction)).toBe(0);
    });

    it('should offer nothing for the canonical spellings', () => {
        expect(findQuickFixes(command + '        authorize Staff and Managers\n', { line: 12 })).toEqual([]);
    });
});
