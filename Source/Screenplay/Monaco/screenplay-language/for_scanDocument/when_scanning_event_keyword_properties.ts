// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';
import { describe, expect, it } from 'vitest';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const names = ['authorize', 'produces', 'reads', 'source', 'key', 'map', 'append', 'from', 'for', 'when', 'namespace', 'generation', 'id', 'description', 'documentation', 'file'];
const lines = [
    'module M', '  feature F', '    slice StateChange S', '      event E',
    ...names.map(name => `        ${name} String`),
    '        @tag String',
    '        tag audit',
    '        description "Not a property"',
];

describe('when scanning event keyword properties', () => {
    it('should index every property the compiler reads', () => {
        const compiled = parse(lines.join('\n'));
        expect(compiled.diagnostics).toEqual([]);
        const properties = compiled.value.modules[0].features[0].slices[0].events[0].properties.map(property => property.name);
        expect(properties).toEqual([...names, 'tag']);
        expect(scanDocument(lines).events[0].properties.map(property => property.name)).toEqual(properties);
    });

    it.each([
        ['file Event.cs', []],
        ['file String', [{ name: 'file', type: 'String' }]],
        ['@file X', [{ name: 'file', type: 'X' }]],
    ])('should distinguish standalone event file metadata from properties in %s', (directive, properties) => {
        const source = [
            'concept X : String',
            'module M', '  feature F', '    slice StateChange S', '      event E',
            `        ${directive}`,
        ];
        const compiled = parse(source.join('\n'));
        expect(compiled.diagnostics).toEqual([]);
        expect(compiled.value.modules[0].features[0].slices[0].events[0].properties
            .map(({ name, type }) => ({ name, type: type.name }))).toEqual(properties);
        expect(scanDocument(source).events[0].properties.map(({ name, type }) => ({ name, type }))).toEqual(properties);
        expect(validateLines(source)).toEqual([]);
    });

    it('should keep inline production metadata reserved', () => {
        const symbols = scanDocument([
            'command C', '  produces event E', '    name String = name',
            '    for String', '    generation Int', '    namespace String',
            '    @tag String = name', '    id String = name',
        ]);
        expect(symbols.events[0].properties.map(property => property.name)).toEqual(['name', 'tag', 'id']);
    });
});
