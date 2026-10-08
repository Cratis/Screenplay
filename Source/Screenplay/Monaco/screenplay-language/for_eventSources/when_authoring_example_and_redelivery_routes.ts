// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { eventSourceCompletions, eventSourceHover } from '../event-source-authoring';
import { mergeSymbols, scanDocument } from '../symbols';

const prefix = 'eventsource A\n  identifier String\n  stream S\n    streamId String\nmodule M\n  feature F\n    slice Automation S\n      event E\n      reaction Observer\n        when E\n';
const parents = ['      example Prior : E', '      specification Recovery\n        when redelivered E to Observer'];
const complete = (source: string) => {
    const lines = source.split('\n');
    return eventSourceCompletions(lines, lines.length - 1, lines.at(-1)!, mergeSymbols(scanDocument(lines)));
};

describe('when authoring example and redelivery routes', () => {
    it.each(parents)('should offer both route forms in %s', parent => {
        const indent = parent.includes('redelivered') ? '          ' : '        ';
        const labels = complete(prefix + parent + '\n' + indent)?.map(entry => entry.label);
        expect(labels).toContain('stream');
        expect(labels).toContain('no stream');
    });

    it.each(parents)('should complete the source and literal id in %s', parent => {
        const indent = parent.includes('redelivered') ? '          ' : '        ';
        expect(complete(prefix + parent + '\n' + indent + 'stream ')?.map(entry => entry.label)).toEqual(['A.S']);
        const entries = complete(prefix + parent + '\n' + indent + 'stream A.S\n' + indent + '  streamId = ');
        expect(entries?.[0].insertText).toContain('value');
        expect(entries?.[0].documentation).toContain('Concrete');
    });

    it.each(parents)('should hover the specification route in %s', parent => {
        const indent = parent.includes('redelivered') ? '          ' : '        ';
        const lines = (prefix + parent + '\n' + indent + 'stream A.S').split('\n');
        const last = lines.at(-1)!;
        const start = last.indexOf('A.S') + 1;
        expect(eventSourceHover(lines, lines.length - 1, start, start + 3)).toContain('Specification event routes are syntax-only');
    });

    it('should not offer routes in a command example', () => {
        const source = prefix + '      command C\n      example Input : C\n        ';
        expect(complete(source)?.some(entry => entry.label === 'stream')).not.toBe(true);
    });
});
