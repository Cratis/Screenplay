// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import { describe, expect, it, vi } from 'vitest';
import { attachDiagnostics } from '../diagnostics';
import { Monaco } from '../language';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const cases = [
    ['command C', '  value Missing?', 'Missing?'],
    ['command C', '  value Missing   optional', 'Missing   optional'],
    ['command C', '\tvalue\tMissing\toptional', 'Missing\toptional'],
    ['command C', '  value Inconnué?', 'Inconnué?'],
    ['command C', '  value Missing', 'Missing'],
    ['produces event Recorded', '  value Missing? = input', 'Missing?'],
    ['produces event Recorded', '  value Missing   optional = input', 'Missing   optional'],
    ['type T', '  value Missing? // keep?', 'Missing?'],
    ['query Q => View', '  by value Missing   optional', 'Missing   optional'],
];

describe('when preserving property type source spans', () => {
    it.each(cases)('should locate the exact type in %s / %s', (header, line, spelling) => {
        const lines = header.startsWith('produces') ? ['command C', '  ' + header, '  ' + line] : [header, line];
        const propertyLine = lines.length - 1;
        const source = lines[propertyLine];
        const diagnostic = validateLines(lines).find(issue => issue.code === 'PLAY0165');
        expect(diagnostic).toBeDefined();
        expect(diagnostic?.line).toBe(propertyLine);
        expect(diagnostic?.startColumn).toBe(source.indexOf(spelling) + 1);
        expect(diagnostic?.endColumn).toBe(source.indexOf(spelling) + spelling.length + 1);
        for (const issue of validateLines(lines)) {
            expect(issue.startColumn).toBeGreaterThan(0);
            expect(issue.endColumn).toBeGreaterThan(issue.startColumn);
        }
        const markers = vi.fn();
        const model = {
            getLinesContent: () => lines, getLanguageId: () => 'screenplay', isDisposed: () => false,
            onDidChangeContent: vi.fn(), onDidChangeLanguage: vi.fn(), onWillDispose: vi.fn(),
        } as unknown as editor.ITextModel;
        const monaco = {
            MarkerSeverity: { Error: 8, Warning: 4, Info: 2 }, MarkerTag: { Deprecated: 2 },
            editor: { getModels: () => [model], setModelMarkers: markers, onDidCreateModel: vi.fn() },
        } as unknown as Monaco;
        expect(() => attachDiagnostics(monaco)).not.toThrow();
        expect(markers.mock.calls[0][2]).toEqual(expect.arrayContaining([expect.objectContaining({
            code: diagnostic?.code, startColumn: diagnostic?.startColumn, endColumn: diagnostic?.endColumn,
        })]));
    });

    it('should keep normalized resolution and display independent of source spelling', () => {
        const property = scanDocument(['command C', '  value Missing?']).commands[0].properties[0];
        expect(property.type).toBe('Missing optional');
        expect(property.typeReference).toEqual({ name: 'Missing', isOptional: true, isCollection: false });
        expect(property.sourceType).toEqual({ text: 'Missing?', startColumn: 9, endColumn: 17 });
    });
});
