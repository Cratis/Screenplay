// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../Structure';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';

const vectors = JSON.parse(readFileSync(resolve(__dirname, '../../Conformance/numeric-source-regressions.json'), 'utf8')) as { integers: { text: string; int: boolean; uint: boolean }[] };
const app = (slice: object): object => ({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, modules: [{ kind: 'ModuleSyntax', name: 'M', isPlacement: false, features: [{ kind: 'FeatureSyntax', name: 'F', isPlacement: false, slices: [{ kind: 'SliceSyntax', name: 'S', type: 'StateChange', ...slice }] }] }] });
const integer = (text: string, unsigned: boolean): string => JSON.stringify(app(unsigned ? { events: [{ kind: 'EventSyntax', name: 'Added', generation: 'TOKEN' }] } : { reactions: [{ kind: 'ReactionSyntax', name: 'R', triggers: [{ kind: 'ReactionTriggerSyntax', source: { kind: 'IntervalTriggerSourceSyntax', amount: 'TOKEN', unit: 'Seconds' } }] }] })).replace('"TOKEN"', text);
const operation = { kind: 'OperationSyntax', name: 'Send', uses: 'External' };
const production = (changes: object = {}): object => ({ kind: 'ProducesSyntax', event: 'Send', inlineOperation: operation, ...changes });
const command = (changes: object): object => app({ commands: [{ kind: 'CommandSyntax', name: 'C', ...changes }] });
const read = (value: object): ApplicationSyntax => decodeExactSyntaxJson(JSON.stringify(value)) as ApplicationSyntax;

describe('when protecting Exact transport boundaries', () => {
    it.each(vectors.integers)('should validate original Int32 and UInt32 structural spellings $text', vector => {
        for (const unsigned of [false, true]) {
            const decode = () => decodeExactSyntaxJson(integer(vector.text, unsigned));
            if (unsigned ? vector.uint : vector.int) expect(decode).not.toThrow();
            else expect(decode).toThrow(InvalidSyntaxJson);
        }
    });
    it.each([
        { when: { kind: 'ComparisonConditionSyntax', left: 'amount', operator: 'Equal', right: { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '1' } } } },
        { event: 'Other' },
        { for: { kind: 'LiteralExpressionSyntax', value: 'global' } },
        { tags: [{ kind: 'TagSyntax', value: { kind: 'LiteralExpressionSyntax', value: 'business' } }] },
        { inlineEvent: { kind: 'EventSyntax', name: 'Send' } },
        { mappings: [{ kind: 'PropertyMappingSyntax', property: 'amount', source: { kind: 'LiteralExpressionSyntax', value: true } }] },
    ])('should reject cross-member production conflicts before restoration and writing', changes => {
        expect(() => read(command({ produces: [production(changes)] }))).toThrow(InvalidSyntaxJson);
        const valid = read(command({ produces: [production()] }));
        Object.assign(valid.modules[0].features[0].slices[0].commands[0].produces[0], changes);
        expect(() => toSyntaxJson(valid)).toThrow(InvalidSyntaxJson);
    });
    it.each([
        { handler: { kind: 'HandlerSyntax', implementation: { kind: 'ImplementationSyntax' }, file: { kind: 'FileReferenceSyntax', path: 'send.cs' }, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'Send();' } } },
        { handler: { kind: 'HandlerSyntax', implementation: { kind: 'ImplementationSyntax', hints: null } } },
        { handler: { kind: 'HandlerSyntax', implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text: ' ' }] } } },
    ])('should share native wrapped-handler invariants on both transport boundaries', change => {
        expect(() => read(command(change))).toThrow(InvalidSyntaxJson);
        const valid = read(command({}));
        Object.assign(valid.modules[0].features[0].slices[0].commands[0], change);
        expect(() => toSyntaxJson(valid)).toThrow(InvalidSyntaxJson);
    });
    it('should normalize only native nullable collections and keep a deep walker safe', () => {
        const restored = read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, types: null, personas: null, seeds: null });
        new class extends ScreenplaySyntaxWalker {}().visitApplication(restored);
        restored.types.should.deep.equal([]);
        restored.modules.should.deep.equal([]);
        expect(() => read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, concepts: null })).toThrow(InvalidSyntaxJson);
        expect(() => read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, modules: null })).toThrow(InvalidSyntaxJson);
        expect(() => read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, eventSources: null })).toThrow(InvalidSyntaxJson);
        expect(() => read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, eventSources: [{ kind: 'EventSourceSyntax', name: 'Orders', streams: null }] })).toThrow(InvalidSyntaxJson);
        expect(() => read(command({ streamCandidates: null }))).toThrow(InvalidSyntaxJson);
    });
    it.each(['projections', 'captures', 'specifications'])('should default omitted nested %s options to Legacy before consistency checks', family => {
        const source = 'numbers exact\nmodule M\n  feature F\n    slice StateChange S\n      projection P\n      capture C\n      specification T\n';
        const parsed = parse(source).value;
        const slice = parsed.modules[0].features[0].slices[0] as unknown as Record<string, { sourceOptions?: unknown }[]>;
        delete slice[family][0].sourceOptions;
        expect(() => toSyntaxJson(parsed)).toThrow(InvalidSyntaxJson);
    });
    it('should refuse omitted options on an Exact standalone root, but retain Legacy omission', () => {
        const value = read({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, seeds: [{ kind: 'SeedSyntax', groups: [{ kind: 'SeedGroupSyntax', eventSourceId: 'global', events: [{ kind: 'SeedEventSyntax', event: 'Added', properties: [{ kind: 'PropertyMappingSyntax', property: 'amount', source: { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '9007199254740993' } } }] }] }] }] });
        delete (value as unknown as { sourceOptions?: unknown }).sourceOptions;
        expect(() => toSyntaxJson(value)).toThrow(InvalidSyntaxJson);
        JSON.stringify(toSyntaxJson(parse('concept Amount : Decimal\n').value)).should.not.contain('sourceOptions');
    });
    it('should reject unknown numeric-looking metadata and duplicate business keys without guessing envelopes', () => {
        expect(() => decodeExactSyntaxJson('{"kind":"ApplicationSyntax","sourceOptions":{"numericMode":"exact"},"location":{"line":1.0000000000000000000001}}')).toThrow(InvalidSyntaxJson);
        expect(() => decodeExactSyntaxJson('{"kind":"ApplicationSyntax","sourceOptions":{"numericMode":"exact","numericMode":"exact"}}')).toThrow(InvalidSyntaxJson);
    });
});
