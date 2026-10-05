// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';

const literal = (value: unknown): unknown => ({ kind: 'LiteralExpressionSyntax', value });
const exact = (value: string): unknown => literal({ literalType: 'ExactNumber', value });
const root = (source: unknown): unknown => ({
    kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' },
    seeds: [{ kind: 'SeedSyntax', groups: [{ kind: 'SeedGroupSyntax', eventSourceId: 'global', events: [{ kind: 'SeedEventSyntax', event: 'Added', properties: [{ kind: 'PropertyMappingSyntax', property: 'amount', source }] }] }] }],
});
const read = (value: unknown): ReturnType<typeof decodeExactSyntaxJson> => decodeExactSyntaxJson(JSON.stringify(value));

describe('when restoring exact syntax', () => {
    it('should restore recursive typed values without guessing business envelopes', () => {
        const value = { kind: 'ObjectExpressionSyntax', members: [
            { kind: 'ObjectMemberSyntax', name: 'literalType', value: literal('ExactNumber') },
            { kind: 'ObjectMemberSyntax', name: 'value', value: { kind: 'ListExpressionSyntax', items: [exact('9007199254740993'), exact('0.0000000000000000000000000001'), literal('1e999999'), literal(null), literal(true)] } },
        ] };
        const restored = read(root(value));
        const wire = JSON.stringify(toSyntaxJson(restored));
        wire.should.contain('9007199254740993');
        wire.should.contain('0.0000000000000000000000000001');
        wire.should.contain('1e999999');
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(wire))).should.equal(wire);
    });

    it.each(['1.0', '-0', '1e0', '01', '1e29', '0.00000000000000000000000000001', '79228162514264337593543950336', 'NaN'])('should reject noncanonical or out-of-domain exact text %s', text => {
        expect(() => read(root(exact(text)))).toThrow(InvalidSyntaxJson);
    });

    it.each(['Int32', 'Int64', 'Decimal', 'Single'])('should refuse old %s envelopes rather than implicitly convert them', literalType => {
        expect(() => read(root(literal({ literalType, value: '1' })))).toThrow(InvalidSyntaxJson);
    });

    it.each([1, 9007199254740992, 0, 1e-28])('should refuse an ordinary numeric token %s in an Exact literal slot', number => {
        expect(() => read(root(literal(number)))).toThrow(InvalidSyntaxJson);
    });

    it('should read e0 numeric tokens as Double and refuse them before source insertion', () => {
        const text = JSON.stringify(root(literal('replace'))).replace('"replace"', '9007199254740993e0');
        expect(() => decodeExactSyntaxJson(text)).toThrow(InvalidSyntaxJson);
    });

    it.each([
        { kind: 'UnknownExpressionSyntax', value: '1' },
        { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '1', extra: true } },
        { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: 1 } },
        { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber' } },
        { kind: 'LiteralExpressionSyntax', value: null, location: { line: 99, column: 4 } },
        { kind: 'SeedSyntax', groups: [] },
        { kind: 'RawExpressionSyntax', text: '1e29' },
    ])('should reject wrong discriminators, unknown members, forged metadata and malformed envelopes', source => {
        expect(() => read(root(source))).toThrow(InvalidSyntaxJson);
    });

    it.each(['legacy', 'Exact', 'exact ', null, 1])('should refuse a malformed or Legacy mode %s', numericMode => {
        const value = { ...root(exact('1')) as object, sourceOptions: { numericMode } };
        expect(() => read(value)).toThrow(InvalidSyntaxJson);
    });

    it.each(['', 'null', 'true', '1', '"text"', '{}', '{', '[', '{"x" 1}', '{x:1}', '[1 2]', '"unterminated', '"bad\\\\x"', '"bad\u0000"', '1e999999999999999999', '[] trailing'])('should refuse malformed lexical JSON or a non-root without guessing a value: %s', text => {
        expect(() => decodeExactSyntaxJson(text)).toThrow(InvalidSyntaxJson);
    });

    it('should reject deep JSON and deep typed syntax independently', () => {
        expect(() => decodeExactSyntaxJson('['.repeat(258) + 'null' + ']'.repeat(258))).toThrow(InvalidSyntaxJson);
        let value: unknown = exact('1');
        for (let index = 0; index < 60; index++) value = { kind: 'ListExpressionSyntax', items: [value] };
        expect(() => read(root(value))).toThrow(InvalidSyntaxJson);
    });

    it('should refuse a missing required member, a wrong scalar shape, and a conflicting nested root mode', () => {
        expect(() => read(root({ kind: 'PathExpressionSyntax' }))).toThrow(InvalidSyntaxJson);
        expect(() => read(root({ kind: 'PathExpressionSyntax', path: [] }))).toThrow(InvalidSyntaxJson);
        const nested = { kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, modules: [{ kind: 'ModuleSyntax', name: 'M', isPlacement: false, features: [{ kind: 'FeatureSyntax', name: 'F', isPlacement: false, slices: [{ kind: 'SliceSyntax', name: 'S', type: 'StateChange', projections: [{ kind: 'ProjectionSyntax', name: 'P', autoMap: 'Inherit', sourceOptions: { numericMode: 'legacy' } }] }] }] }] };
        expect(() => read(nested)).toThrow(InvalidSyntaxJson);
    });

    it('should validate typed numeric structural ranges instead of accepting malformed node scalars', () => {
        const node = (amount: unknown): unknown => ({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, modules: [{ kind: 'ModuleSyntax', name: 'M', isPlacement: false, features: [{ kind: 'FeatureSyntax', name: 'F', isPlacement: false, slices: [{ kind: 'SliceSyntax', name: 'S', type: 'Automation', reactions: [{ kind: 'ReactionSyntax', name: 'R', triggers: [{ kind: 'ReactionTriggerSyntax', source: { kind: 'IntervalTriggerSourceSyntax', amount, unit: 'Seconds' } }] }] }] }] }] });
        expect(() => read(node(2147483648))).toThrow(InvalidSyntaxJson);
        expect(() => read(node(-2147483649))).toThrow(InvalidSyntaxJson);
        expect(() => read(node(1.5))).toThrow(InvalidSyntaxJson);
        read(node(1)).kind.should.equal('ApplicationSyntax');
    });

    it('should reject blank implementation hints and conflicting phase payloads at restoration', () => {
        const operation = (phase: unknown): unknown => ({ kind: 'ApplicationSyntax', sourceOptions: { numericMode: 'exact' }, modules: [{ kind: 'ModuleSyntax', name: 'M', isPlacement: false, features: [{ kind: 'FeatureSyntax', name: 'F', isPlacement: false, slices: [{ kind: 'SliceSyntax', name: 'S', type: 'StateChange', operations: [{ kind: 'OperationSyntax', name: 'Send', uses: 'External', execute: phase }] }] }] }] });
        expect(() => read(operation({ kind: 'OperationPhaseSyntax', implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text: ' ' }] } }))).toThrow(InvalidSyntaxJson);
        expect(() => read(operation({ kind: 'OperationPhaseSyntax', file: { kind: 'FileReferenceSyntax', path: 'send.cs' }, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'Send();' } }))).toThrow(InvalidSyntaxJson);
    });

    it('should reject conflicting nested programmatic source options and unconverted object slots', () => {
        const value = read(root(exact('1')));
        const nested = { kind: 'CaptureSyntax', sourceOptions: { numericMode: 'legacy' }, location: { line: 1, column: 1 } };
        const conflicted = { ...value, extraChild: nested };
        expect(() => toSyntaxJson(conflicted)).toThrow(InvalidSyntaxJson);
        const old = { ...root(literal({ literalType: 'Decimal', value: '1' })) as object, location: { line: 1, column: 1 }, kind: 'ApplicationSyntax' };
        expect(() => toSyntaxJson(old)).toThrow(InvalidSyntaxJson);
    });

    it('should reject duplicate members before any restoration', () => {
        expect(() => decodeExactSyntaxJson('{"kind":"ApplicationSyntax","kind":"ApplicationSyntax","sourceOptions":{"numericMode":"exact"}}')).toThrow(InvalidSyntaxJson);
    });

    it('should refuse exact tags on a Legacy programmatic root and refuse old-number insertion on an Exact root', () => {
        const value = read(root(exact('9007199254740993')));
        const edited = value as unknown as { sourceOptions: { numericMode: string } };
        edited.sourceOptions = { numericMode: 'legacy' };
        expect(() => toSyntaxJson(value)).toThrow(InvalidSyntaxJson);
        const ordinary = { ...root(literal(1)) as object, location: { line: 1, column: 1 } };
        expect(() => toSyntaxJson(ordinary as typeof value)).toThrow(InvalidSyntaxJson);
    });

    it('should assign fresh locations without inventing token spans', () => {
        const restored = read(root(exact('1')));
        restored.location.should.deep.equal({ line: 1, column: 1 });
        JSON.stringify(restored).should.not.contain('rawLength');
        JSON.stringify(restored).should.not.contain('sourceLength');
    });
});
