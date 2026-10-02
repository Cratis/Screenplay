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
    it('should retain standalone property-shaped id and documentation names', () => expect(scanDocument(['event Done', '  id String', '  documentation String']).events[0].properties.map(property => property.name)).toEqual(['id', 'documentation']));
});
