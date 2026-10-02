// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { planCompletions } from '../completion-planner';
import { queryItems } from '../completion-items';
import { fileReferences } from '../file-references';
import { hoverContent } from '../hover-content';
import { destinationHints } from '../production-destinations';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const prefix = ['module M', '  feature F', '    slice StateChange S', '      command C'];

describe('when editing optional values', () => {
    it('should store optional and collection flags independently of spelling', () => {
        for (const marker of [' optional', '?']) {
            const symbols = scanDocument([...prefix, `        notes String[]${marker}`, `      query Q => observable M.Note${marker}`]);
            expect(symbols.commands[0].properties[0].typeReference).toEqual({ name: 'String', isOptional: true, isCollection: true });
            expect(symbols.queries[0].returnTypeReference).toEqual({ name: 'M.Note', isOptional: true, isCollection: false });
        }
    });

    it('should never offer optional identifiers as production destinations', () => {
        expect(destinationHints([...prefix, '        id Uuid optional identifier', '        produces event E'])).toEqual([]);
    });

    it('should point the spelling diagnostic at the complete type exactly once', () => {
        const lines = ['type T', '  description String[]? // comment?', '  optional String optional'];
        const diagnostics = validateLines(lines).filter(issue => issue.code === 'PLAY0479');
        expect(diagnostics).toHaveLength(1);
        expect(diagnostics[0]).toMatchObject({ severity: 'information', line: 1, startColumn: 15, endColumn: 24 });
    });

    it('should explain a type accidentally named optional', () => {
        expect(validateLines(['type T', '  value optional'])[0].message).toContain("did you forget the type before 'optional'?");
    });

    it('should preserve file reference disambiguation', () => {
        expect(fileReferences(['type T', '  file String optional', '  file Models/T.cs']).map(reference => reference.path)).toEqual(['Models/T.cs']);
    });

    it('should offer only canonical optional completion', () => {
        const lines = [...prefix, '        note String '];
        const plan = planCompletions(lines, 4, lines[4]);
        expect(plan).toMatchObject({ kind: 'entries', entries: [{ label: 'optional', insertText: 'optional' }] });
        expect(queryItems.find(item => item.label === 'filter')?.insertText).toBe('filter ${1:param} ${2:Type} optional');
    });

    it('should explain the modifier without claiming a name', () => {
        expect(hoverContent(['type T', '  value String optional'], 1, 'optional', 16, 24)).toContain('Allows a value to be absent');
        expect(hoverContent(['type T', '  optional String'], 1, 'optional', 3, 11)).toBeNull();
        expect(hoverContent(['type T', '  value optional'], 1, 'optional', 9, 17)).toBeNull();
    });

    it('should keep optionality validation linear as the document doubles', () => {
        const measure = (count: number) => {
            const lines = [...prefix, ...Array.from({ length: count }, (_, index) => `        p${index} String?`)];
            const filters = vi.spyOn(Array.prototype, 'filter');
            const searches = vi.spyOn(Array.prototype, 'find');
            try {
                expect(validateLines(lines).filter(issue => issue.code === 'PLAY0479')).toHaveLength(count);
                return [...filters.mock.contexts, ...searches.mock.contexts].reduce<number>((sum, value) => sum + (value as unknown[]).length, 0);
            } finally {
                filters.mockRestore();
                searches.mockRestore();
            }
        };
        const small = measure(500);
        expect(measure(1000)).toBeLessThan(small * 2.2);
    });
});
