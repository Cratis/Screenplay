// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

function document(count: number): string {
    return [...Array.from({ length: count }, (_, index) => `import Other.Imported${index}`),
        'module Projects', '  feature Naming', ...Array.from({ length: count }, (_, index) => [
            `    slice StateChange Rename${index}`, `      command Rename${index}`,
            '        projectId Uuid identifier', '        name String',
            `        produces event Renamed${index}`, '          name String = name',
        ]).flat()].join('\n');
}

// Count collection traversal sizes rather than timings. This catches rescanning the
// event catalog or the name imports for each production in either compilation path.
function collectionWork(action: () => void): number {
    const filters = vi.spyOn(Array.prototype, 'filter');
    const searches = vi.spyOn(Array.prototype, 'some');
    try {
        action();
        return [...filters.mock.contexts, ...searches.mock.contexts]
            .reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0);
    } finally {
        filters.mockRestore();
        searches.mockRestore();
    }
}

describe('when validating many inline events', () => {
    it.each([
        ['document', false], ['application', false], ['document', true], ['application', true],
    ] as const)('should keep %s compilation collection work linear with identifier copies: %s', (path, copyingIdentifier) => {
        const measure = (count: number) => {
            const source = copyingIdentifier ? document(count).replaceAll('name String = name', 'projectId Uuid = projectId') : document(count);
            let diagnostics = -1;
            const units = collectionWork(() => {
                diagnostics = (path === 'document' ? parse(source) : compileApplication(new Map([['model.play', source]]))).diagnostics.length;
            });
            expect(diagnostics).toBe(copyingIdentifier ? count : 0);
            return units;
        };
        const small = measure(100);
        const large = measure(200);
        expect(small).toBeGreaterThan(0);
        expect(large).toBeLessThan(small * 2.2);
    });

    it('should retain collision diagnostics for imports and inline siblings', () => {
        const source = document(2).replace('import Other.Imported0', 'import Other.Renamed0').replace('event Renamed1', 'event Renamed0');
        for (const result of [parse(source), compileApplication(new Map([['model.play', source]]))]) {
            expect(result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)).toEqual(['PLAY0473@9', 'PLAY0473@15']);
        }
    });
});
