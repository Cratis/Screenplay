// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const prefix = 'eventsource A\n  stream S\n    streamId\n      one String\n      two String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        stream A.S\n          streamId\n';

describe('when authoring composite stream ids', () => {
    it('should parse composite declarations and mappings in authored order', () => {
        const result = parse(prefix + '            two = value\n            one = "literal"');
        expect(result.diagnostics).toEqual([]);
        expect(JSON.stringify(toSyntaxJson(result.value))).toContain('EventStreamIdPartSyntax');
    });
    it.each([
        ['streamId\n      one String', 'PLAY0503'],
        ['streamId\n      one String\n      one String', 'PLAY0503'],
        ['streamId\n      one String identifier\n      two String', 'PLAY0503'],
        ['streamId\n      one Decimal\n      two String', 'PLAY0506'],
        ['streamId\n      one String\n      two String\n    streamId String', 'PLAY0503'],
        ['streamId', 'PLAY0503'],
    ])('should refuse invalid declaration %s', (body, code) => {
        expect(parse('eventsource A\n  stream S\n    ' + body).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
    it.each([
        'one = value',
        'one = value\n            two = value\n            extra = value',
        'one = value\n            two = value\n            two = value',
        'one = value\n            two = 42',
        'one = value\n            two = ""',
        'one = value\n            two = absent',
    ])('should refuse invalid mapping %s', mappings => {
        expect(parse(prefix + '            ' + mappings).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
    });
    it('should visit declarations, types and mappings and refuse unrepresentable shapes', () => {
        const result = parse(prefix + '            one = "a"\n            two = "b"').value;
        const stream = result.eventSources![0].streams[0];
        const part = stream.streamIdParts[0];
        const route = result.modules[0].features[0].slices[0].commands[0].stream!;
        class Walker extends ScreenplaySyntaxWalker {
            nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const walker = new Walker();
        walker.visitApplication(result);
        expect(walker.nodes.filter(node => node.kind === 'EventStreamIdPartSyntax')).toHaveLength(2);
        for (const node of [
            { ...stream, streamIdParts: null },
            { ...stream, streamId: part.type },
            { ...part, name: 'bad-name' },
            { ...part, type: null },
            { ...part, type: { ...part.type, isOptional: true } },
            { ...part, type: { ...part.type, isCollection: true } },
            { ...part, type: { ...part.type, name: 'bad-name' } },
            { ...route, streamIdParts: null },
            { ...route, streamId: { ...route.streamIdParts[0], property: 'streamId' } },
            { ...route, streamIdParts: [{ ...route.streamIdParts[0], property: 'bad-name' }] },
        ]) expect(() => toSyntaxJson(node)).toThrow();
    });
    it('should accept whitespace literals', () => {
        expect(parse(prefix + '            one = " "\n            two = value').diagnostics).toEqual([]);
    });
});
