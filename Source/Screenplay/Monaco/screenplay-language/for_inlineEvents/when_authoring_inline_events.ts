// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { destinationHints } from '../production-destinations';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const source = [
    'command Rename', '  projectId Uuid identifier', '  name String', '  produces event Renamed',
    '    description "A new name"', '    documentation', '      ```markdown', '      event NotADeclaration', '      ```',
    '    @tag String = name', '    name String = name',
];

describe('when authoring inline events', () => {
    it('should index the event and its typed properties, not its prose', () => {
        const symbols = scanDocument(source);
        expect(symbols.events.map(event => event.name)).toEqual(['Renamed']);
        expect(symbols.events[0].properties.map(property => property.name)).toEqual(['tag', 'name']);
        expect(symbols.commands[0].properties.map(property => property.name)).toEqual(['projectId', 'name']);
    });
    it('should validate its metadata without false positives', () => expect(validateLines(source)).toEqual([]));
    it.each([
        ['    documentation "details"'],
        ['    documentation'],
        ['    documentation', '      ```text', '      Details', '      ```'],
        ['    documentation', '    ```markdown', '    Details', '    ```'],
        ['    documentation', '      ```markdown', '      ', '      ```'],
        ['    documentation', '      ```markdown', '      Details'],
    ])('should reject malformed documentation without searching later declarations for its fence', (...body) => {
        const lines = ['command Rename', '  produces event Renamed', ...body];
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0477').map(issue => issue.line)).toEqual([2]);
    });
    it('should distinguish adjacent complete documentation blocks from malformed directives', () => {
        const lines = ['event First', '  documentation "details"', 'event Second', '  documentation',
            '    ```markdown', '    Details', '    ```', 'event Third', '  documentation',
            '    ```markdown', '    ', '    ```', 'event Fourth', '  documentation', '',
            '    ```markdown', '    More details', '    ```'];
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0477').map(issue => issue.line)).toEqual([1, 8]);
    });
    it('should offer event metadata and typed mappings inside the body', () => {
        const plan = planCompletions([...source, '    '], source.length, '    ');
        expect(plan.kind).toBe('entries');
        if (plan.kind === 'entries') expect(plan.entries.map(entry => entry.label)).toEqual(expect.arrayContaining(['description', 'documentation', 'id', 'tag', 'for', 'property mapping']));
    });
    it('should show the implicit command identifier', () => expect(destinationHints(source).map(hint => hint.label)).toEqual(['for projectId']));
    it('should suppress hints for uncertain mixed destinations', () => expect(destinationHints([...source, '  produces Another', '    for otherId'])).toEqual([]));
    it('should suppress hints for incomplete and repeated explicit destinations', () => {
        expect(destinationHints([...source, '    for'])).toEqual([]);
        expect(destinationHints([...source, '    for projectId', '    for projectId'])).toEqual([]);
    });
    it('should not hint a header that has no event name', () => expect(destinationHints(['command Rename', '  produces'])).toEqual([]));
    it('should explain the new declaration form', () => expect(hoverContent(source, 3, 'produces', 3, 11)).toContain('produces event'));
    it('should preserve information severity for redundant pins', () => expect(validateLines([...source, '    id "Renamed"']).find(issue => issue.code === 'PLAY0471')?.severity).toBe('information'));
    it.each(['namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred'])('should reject %s as system metadata', keyword => {
        expect(validateLines([...source, `    ${keyword} = name`]).map(issue => issue.code)).toContain('PLAY0476');
    });
    it('should warn about identifier duplication', () => expect(validateLines([...source, '    projectId Uuid = projectId']).find(issue => issue.code === 'PLAY0469')?.severity).toBe('warning'));
    it('should reject an inline event in a reaction', () => expect(validateLines(['reaction React', '  every 1 day', '    produces event Done']).map(issue => issue.code)).toContain('PLAY0474'));
    it('should require explicit destinations for mixed inline and plain omissions', () => {
        const lines = [...source, '  produces Legacy // retained allocation'];
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0470').map(issue => issue.message)).toContain("Production 'Legacy' must state for explicitly when destinations differ.");
        expect(destinationHints(lines)).toEqual([]);
    });
    it('should suppress legacy allocation hints when a sibling can promote a destination', () => {
        expect(destinationHints(['command Rename', '  projectId Uuid identifier', '  produces First', '    for projectId', '  produces Legacy'])).toEqual([]);
    });
    it.each([
        ['  produces First', '    name = name', '      for projectId', '  produces Legacy'],
        ['  produces First', '      name = name', '    for projectId', '  produces Legacy'],
        ['    produces First', '      name = name', '        for projectId', '  produces Legacy'],
        ['  produces when name != ""', '    First', '      name = name', '        for projectId', '  produces Legacy'],
    ])('should not hint allocation when a differently indented destination is promoted', (...productions) => {
        const lines = ['command Rename', '  projectId Uuid identifier', '  name String', ...productions];
        expect(scanDocument(lines).commands[0].produces?.[0].target).toBe('projectId');
        expect(destinationHints(lines)).toEqual([]);
    });
    it('should see deeper destinations after fenced event documentation', () => {
        const lines = ['command Rename', '  projectId Uuid identifier', '  produces event Renamed',
            '    documentation', '      ```markdown', '      Details', '      ```', '      for otherId', '  produces Legacy'];
        expect(scanDocument(lines).commands[0].produces?.[0].target).toBe('otherId');
        expect(destinationHints(lines)).toEqual([]);
    });
    it('should suppress hints when a sibling production cannot be classified', () => {
        expect(destinationHints(['command Rename', '  projectId Uuid identifier', '  produces First', '  produces event'])).toEqual([]);
        expect(destinationHints(['command Rename', '  projectId Uuid identifier', '  produces event'])).toEqual([]);
        expect(destinationHints(['command Rename', '  produces First', '  produces when'])).toEqual([]);
    });
    it('should place a destination hint before a trailing comment', () => {
        const lines = ['command Rename', '  projectId Uuid identifier', '  produces event Renamed // note'];
        expect(destinationHints(lines)).toEqual([{ line: 2, column: '  produces event Renamed'.length + 1, label: 'for projectId' }]);
    });
    it('should report duplicate typed properties at the repeated mapping', () => {
        const lines = [...source, '      name Uuid = otherId // duplicate'];
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0168').map(issue => ({ line: issue.line, severity: issue.severity })))
            .toEqual([{ line: source.length, severity: 'error' }]);
    });
    it('should treat escaped and unescaped inline property names as the same name', () => {
        expect(validateLines([...source, '    @name String = name']).filter(issue => issue.code === 'PLAY0168').map(issue => issue.line)).toEqual([source.length]);
    });
    it('should detect collisions with commented imports in the document', () => {
        expect(validateLines(['import Other.Renamed // note', ...source]).filter(issue => issue.code === 'PLAY0473').map(issue => issue.line)).toEqual([4]);
    });
    it('should detect collisions with commented imports from other files', () => {
        const application = scanDocument(['import Other.Renamed // note']);
        expect(validateLines(source, { application }).filter(issue => issue.code === 'PLAY0473').map(issue => issue.line)).toEqual([3]);
    });
    it('should not scan imports inside event documentation', () => {
        const lines = source.map(line => line === '      event NotADeclaration' ? '      import Other.Renamed // prose' : line);
        expect(scanDocument(lines).imports).toEqual([]);
        expect(validateLines(lines)).toEqual([]);
    });
    it('should hint allocation when all plain productions omit destinations', () => {
        expect(destinationHints(['command Rename', '  produces First', '  produces Legacy', 'event First', 'event Legacy']).map(hint => hint.label)).toEqual(['for <new event source>', 'for <new event source>']);
    });
    it('should analyze commented headers, destinations, mappings and metadata like the compiler', () => {
        const lines = source.map(line => line === '    documentation' ? `${line} // details` : line);
        lines.push('    projectId Uuid = projectId // note', '  produces Other // sibling', '    for projectId // same identifier', 'event Other');
        expect(validateLines(lines).filter(issue => ['PLAY0470', 'PLAY0477'].includes(issue.code ?? ''))).toEqual([]);
        expect(validateLines(lines).find(issue => issue.code === 'PLAY0469')?.severity).toBe('warning');
        expect(destinationHints(lines).map(hint => hint.label)).toEqual(['for projectId']);
    });
    it('should suppress hints when a commented sibling targets another source', () => {
        const lines = [...source, '  produces Other // sibling', '    for otherId // other source'];
        expect(validateLines(lines).some(issue => issue.code === 'PLAY0470')).toBe(true);
        expect(destinationHints(lines)).toEqual([]);
    });
    it('should preserve comment markers inside strings and templates and ignore fences', () => {
        const lines = [...source, '    url String = "https://example.org" // url', '    template String = `https://{name}` // template'];
        expect(scanDocument(lines).commands[0].produces?.[0].mappings.map(mapping => mapping.source)).toContain('"https://example.org"');
        expect(scanDocument(lines).commands[0].produces?.[0].mappings.map(mapping => mapping.source)).toContain('`https://{name}`');
        expect(destinationHints(lines).map(hint => hint.label)).toEqual(['for projectId']);
        const fenced = ['command Rename', '  documentation', '    ```markdown', '  produces event Fake', '    for otherId', '    ```'];
        expect(destinationHints(fenced)).toEqual([]);
        expect(validateLines(fenced).filter(issue => issue.code?.startsWith('PLAY047'))).toEqual([]);
    });
    it('should keep id hover event-specific while sharing documentation hover', () => {
        expect(hoverContent(['event Done', '  id "Old"'], 1, 'id', 3, 5)).toContain('rename');
        expect(hoverContent(['event Done', '  documentation'], 1, 'documentation', 3, 16)).not.toBeNull();
        for (const line of ['  id String', '  key id', '  name String = id', '  id String = name']) {
            const start = line.indexOf('id') + 1;
            expect(hoverContent(['event Done', line], 1, 'id', start, start + 2)).toBeNull();
        }
        expect(hoverContent(['command Done', '  documentation'], 1, 'documentation', 3, 16)).toContain('command');
    });
    it('should retain standalone property-shaped id and documentation names', () => expect(scanDocument(['event Done', '  id String', '  documentation String']).events[0].properties.map(property => property.name)).toEqual(['id', 'documentation']));
});
