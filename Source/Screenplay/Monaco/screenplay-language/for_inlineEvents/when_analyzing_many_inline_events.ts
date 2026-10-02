// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as context from '../document-context';
import { destinationHints } from '../production-destinations';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

function document(count: number): string[] {
    return ['module Projects', '  feature Naming', ...Array.from({ length: count }, (_, index) => [
        `    slice StateChange Rename${index}`,
        `      command Rename${index}`,
        '        projectId Uuid identifier',
        '        name String',
        `          produces event Renamed${index}`,
        '            name String = name',
        '            // mapping complete',
    ]).flat()];
}

// Count actual work, not elapsed time: indentation visits catch ancestry walks,
// replacements catch whole-document normalization per event, and filter input
// sizes catch rebuilding/scanning the event catalog for every declaration.
function work(action: () => void): number {
    const indentation = vi.spyOn(context, 'indentOf');
    const replacements = vi.spyOn(String.prototype, 'replace');
    const filters = vi.spyOn(Array.prototype, 'filter');
    try {
        action();
        return indentation.mock.calls.length + replacements.mock.calls.length +
            filters.mock.contexts.reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0);
    } finally {
        indentation.mockRestore();
        replacements.mockRestore();
        filters.mockRestore();
    }
}

describe('when analyzing many inline events', () => {
    it.each(['validation', 'destination hints'])('should keep %s work linear', path => {
        const measure = (count: number) => {
            const lines = document(count);
            const application = scanDocument(Array.from({ length: count }, (_, index) => `import Other.Imported${index}`));
            let results = 0;
            const units = work(() => {
                results = path === 'validation' ? validateLines(lines, { application }).length : destinationHints(lines).length;
            });
            expect(results).toBe(path === 'validation' ? 0 : count);
            return units;
        };
        const small = measure(100);
        const large = measure(200);
        expect(small).toBeGreaterThan(0);
        expect(large).toBeLessThan(small * 2.2);
    });
});
