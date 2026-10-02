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
    it('should hint allocation when all plain productions omit destinations', () => {
        expect(destinationHints(['command Rename', '  produces First', '  produces Legacy']).map(hint => hint.label)).toEqual(['for <new event source>', 'for <new event source>']);
    });
    it('should analyze commented headers, destinations, mappings and metadata like the compiler', () => {
        const lines = source.map(line => line === '    documentation' ? `${line} // details` : line);
        lines.push('    projectId Uuid = projectId // note', '  produces Other // sibling', '    for projectId // same identifier');
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
    it('should show metadata hover only on event directives', () => {
        expect(hoverContent(['event Done', '  id "Old"'], 1, 'id', 3, 5)).toContain('rename');
        expect(hoverContent(['event Done', '  documentation'], 1, 'documentation', 3, 16)).not.toBeNull();
        for (const line of ['  id String', '  key id', '  name String = id', '  id String = name']) {
            const start = line.indexOf('id') + 1;
            expect(hoverContent(['event Done', line], 1, 'id', start, start + 2)).toBeNull();
        }
        expect(hoverContent(['command Done', '  documentation'], 1, 'documentation', 3, 16)).toBeNull();
    });
    it('should retain standalone property-shaped id and documentation names', () => expect(scanDocument(['event Done', '  id String', '  documentation String']).events[0].properties.map(property => property.name)).toEqual(['id', 'documentation']));
});
