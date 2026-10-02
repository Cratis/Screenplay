// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { destinationHints } from '../production-destinations';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

describe('when indexing command production ownership', () => {
    it.each(['validate', 'handler', 'concurrency', 'name'])('should keep a deeper production after the %s property in its command', name => {
        const lines = ['command Rename', '  projectId Uuid identifier', `  ${name} String`,
            '    produces event Renamed', '      value String = "renamed"'];
        expect(scanDocument(lines).commands[0].produces?.map(production => production.name)).toEqual(['Renamed']);
        expect(validateLines(lines)).toEqual([]);
        expect(destinationHints(lines).map(hint => hint.label)).toEqual(['for projectId']);
    });

    it.each(['validate', 'handler', 'concurrency'])('should not hint allocation after a deeper destination beneath the %s property', name => {
        const lines = ['command Rename', '  projectId Uuid identifier', `  ${name} String // a property, not a block`,
            '    produces First', '      for projectId', '  produces Legacy'];
        expect(scanDocument(lines).commands[0].produces?.map(production => [production.name, production.target]))
            .toEqual([['First', 'projectId'], ['Legacy', undefined]]);
        expect(destinationHints(lines)).toEqual([]);
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0478').map(issue => issue.line)).toEqual([5]);
    });

    it.each(['validate', 'handler', 'concurrency'])('should suppress hints for an unclassifiable production beneath the %s property', name => {
        expect(destinationHints(['command Rename', '  projectId Uuid identifier', `  ${name} String`,
            '    produces event', '  produces Legacy'])).toEqual([]);
    });

    it.each(['validate', 'validate csharp', 'handler', 'concurrency'])('should keep the %s block opaque to command ownership', directive => {
        const lines = ['command Rename', `  ${directive}`, '    ghost Uuid identifier', '    produces First', '      for ghost', '  produces Legacy'];
        const command = scanDocument(lines).commands[0];
        expect(command.properties).toEqual([]);
        expect(command.produces?.map(production => production.name)).toEqual(['Legacy']);
        expect(destinationHints(lines).map(hint => hint.line)).toEqual([5]);
        expect(validateLines(lines).filter(issue => issue.code === 'PLAY0478')).toEqual([]);
    });
});
