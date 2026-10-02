// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const prefix = 'module Projects\n  feature Recording\n    slice StateChange Record\n      command Record\n        produces event Recorded\n';

// Include both searched array sizes and set lookups: counting just the latter would
// miss a regression that builds a set but still scans the ordered property list.
function work(source: string): number {
    const searches = vi.spyOn(Array.prototype, 'some');
    const membership = vi.spyOn(Set.prototype, 'has');
    const insertions = vi.spyOn(Set.prototype, 'add');
    try {
        const result = parse(source);
        expect(result.diagnostics).toEqual([]);
        return searches.mock.contexts.reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0) +
            membership.mock.calls.length + insertions.mock.calls.length;
    } finally {
        searches.mockRestore();
        membership.mockRestore();
        insertions.mockRestore();
    }
}

describe('when scaling one inline event with many typed mappings', () => {
    it('should keep duplicate property detection linear', () => {
        const measure = (count: number) => work(prefix + Array.from({ length: count }, (_, index) => `          value${index} String = "value"`).join('\n'));
        const small = measure(1000);
        const large = measure(2000);
        expect(small).toBeGreaterThan(0);
        expect(large).toBeLessThan(small * 2.2);
    });

    it('should retain duplicate diagnostics, ordinal names and mapping order', () => {
        const result = parse(prefix + '          valueA String = "first"\n          valuea String = "second"\n          @valueA String = "third"\n          valueA String = "fourth"');
        expect(result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)).toEqual(['PLAY0168@8', 'PLAY0168@9']);
        expect(result.diagnostics.map(diagnostic => diagnostic.message)).toEqual(Array(2).fill("Event 'Recorded' already declares property 'valueA'"));
        const production = result.value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(production.inlineEvent?.properties.map(property => property.name)).toEqual(['valueA', 'valuea', 'valueA', 'valueA']);
        expect(production.mappings.map(mapping => mapping.property)).toEqual(['valueA', 'valuea', 'valueA', 'valueA']);
    });
});
