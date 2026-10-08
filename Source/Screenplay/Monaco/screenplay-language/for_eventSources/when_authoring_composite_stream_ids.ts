// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { eventSourceCompletions, eventSourceHover } from '../event-source-authoring';
import { responseTokens } from '../response-tokens';
import { mergeSymbols, scanDocument } from '../symbols';

const declarations = 'concept Period : String\neventsource A\n  stream S\n    streamId\n      key Uuid\n      period Period\n';
const command = declarations + 'command C\n  id Uuid identifier\n  month Period\n  wrong String\n  stream A.S\n    streamId\n      key = id\n      ';
const complete = (text: string) => {
    const lines = text.split('\n');
    return eventSourceCompletions(lines, lines.length - 1, lines.at(-1)!, mergeSymbols(scanDocument(lines)));
};

describe('when authoring composite stream ids', () => {
    it('should offer only unmapped named parts', () => {
        expect(complete(command)?.map(entry => entry.label)).toEqual(['period']);
        expect(complete(command)?.[0].insertText).toBe('period = ');
    });
    it('should offer nominally compatible command sources for the selected part', () => {
        expect(complete(command + 'period = ')?.map(entry => entry.label)).toEqual(['month']);
    });
    it('should offer literal mappings for specification parts', () => {
        const source = declarations + 'specification X\n  then E\n    stream A.S\n      streamId\n        key = "00000000-0000-0000-0000-000000000000"\n        ';
        expect(complete(source)?.map(entry => entry.label)).toEqual(['period']);
        expect(complete(source)?.[0].insertText).toContain('period = "');
    });
    it('should offer scalar subset types underneath declaration headers', () => {
        expect(complete(declarations + 'eventsource B\n  stream S\n    streamId\n      one ')?.map(entry => entry.label)).toEqual(['String', 'Uuid', 'Period']);
    });
    it('should show composite schemas, part types and authored source hover', () => {
        const lines = (command + 'period = month').split('\n');
        expect(eventSourceHover(lines, 10, 10, 13)).toContain('key Uuid');
        expect(eventSourceHover(lines, 5, 14, 20)).toContain('Period');
        expect(eventSourceHover(lines, 13, 16, 21)).toContain('Command source for authored stream id');
        expect(eventSourceHover(lines, 13, 7, 13)).toContain('period');
    });
    it('should classify headers, part names and part types without changing payload tokens', () => {
        const tokens = responseTokens(declarations.split('\n'));
        expect(tokens).toContainEqual({ line: 3, column: 4, length: 8, type: 0 });
        expect(tokens).toContainEqual({ line: 4, column: 6, length: 3, type: 1 });
        expect(tokens).toContainEqual({ line: 4, column: 10, length: 4, type: 2 });
    });
});
