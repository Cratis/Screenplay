// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { parsePlacedDocuments } from '../Files/PlayApplicationAssembly';
import { EventSourceReadConfidence } from '../Syntax/EventSourceReadConfidence';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const clean = 'eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions';
const application = parse(clean).value;
const source = application.eventSources![0];
const stream = source.streams[0];
const command = application.modules[0].features[0].slices[0].commands[0];
const write = (node: unknown) => toSyntaxJson(node as SyntaxNode);

const invalidNames = ['Account𐐀', 'Account\uD801', 'Account\uDC00', 'Account\u0903', 'Account\u00B2'];
const validNames = ['Accountß', 'Accountα', 'Account\u01C5', 'Account\u02B0', 'Account\u4E00', 'Account\u0301', 'Account\u0661', 'Account\u203F'];

describe('when guarding source stream contracts', () => {
    it.each([
        ['module Broken\n  description\n    ```text\neventsource Account\n  stream Other', false, 'PLAY0164'],
        ['module Broken\n  description\n    ```text\neventsource Account\n  stream Other\n    ```', true, null],
        ['module Broken\n  feature F\n    slice StateChange S\n      command C\n        invalid directive', true, null],
        ['module Broken\n  feature F\n    slice StateChange S\n      command C\n        handler\n          ```csharp\neventsource Account', false, 'PLAY0164'],
    ])('should distinguish source extent for %s', (tail, complete, code) => {
        const documents = [{ path: 'clean.play', source: clean, placement: [] }, { path: 'other.play', source: tail, placement: [] }];
        const result = parsePlacedDocuments(documents);
        expect(result.sourceInventoryComplete).toBe(complete);
        expect(result.physicalEventSources.map(entry => entry.source.name)).toEqual(['Account']);
        expect(new EventSourceReadConfidence(result.physicalEventSources, result.sourceInventoryComplete).resolve('Account', 'Transactions').state).toBe(complete ? 'unique' : 'incomplete');
        if (code) expect(result.diagnostics).toContainEqual(expect.objectContaining({ code, location: expect.objectContaining({ path: 'other.play' }) }));
    });
    it.each(invalidNames)('should reject unsupported UTF16 names %s in all new declarations and references', name => {
        expect(parse(`eventsource ${name}\n  stream Transactions`).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0503');
        expect(parse(`eventsource Account\n  stream ${name}`).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0503');
        for (const node of [{ ...source, name }, { ...stream, name }, { ...command.stream!, eventSource: name }, { ...command.stream!, stream: name }]) expect(() => write(node)).toThrow();
        if (name.includes('𐐀') || /[\uD800-\uDFFF]/.test(name)) {
            for (const node of [{ ...source, identifier: { kind: 'TypeRefSyntax', name, isCollection: false, isOptional: false } }, { ...stream, streamId: { kind: 'TypeRefSyntax', name, isCollection: false, isOptional: false } }]) expect(() => write(node)).toThrow();
        }
    });
    it.each(validNames)('should retain compiler-supported BMP continuation %s', name => {
        const parsed = parse(clean.replaceAll('Account', name).replaceAll('Transactions', name));
        expect(parsed.success).toBe(true);
        expect(parsed.value.modules[0].features[0].slices[0].commands[0].stream?.eventSource).toBe(name);
        expect(() => write(parsed.value)).not.toThrow();
    });
    it.each([null, [null], {}, [stream], [{}], [42]])('should reject malformed application source collections %j', eventSources => {
        expect(() => write({ ...application, eventSources })).toThrow();
    });
    it.each([null, [null], {}, [source], [{}], [42]])('should reject malformed nested stream collections %j', streams => expect(() => write({ ...source, streams })).toThrow());
    it.each([null, [null], {}, [source], [{}], [42]])('should reject malformed retained route collections %j', streamCandidates => expect(() => write({ ...command, streamCandidates })).toThrow());
    it('should leave legacy omission bytes unchanged', () => {
        const { eventSources: _sources, ...old } = parse('').value;
        expect(JSON.stringify(write(old))).toBe(JSON.stringify(write({ ...old, eventSources: [] })));
    });
});
