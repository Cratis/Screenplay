// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse, parseCaptureSource, parseProjectionSource, parseWithLanguages } from '../../ScreenplayCompiler';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { mergeDocuments } from '../../Files/PlayFolderMerge';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { ComparisonConditionSyntax } from '../../Syntax/Conditions';
import { ClaimConditionSyntax } from '../../Syntax/Policies';

const vectors = JSON.parse(readFileSync(resolve(__dirname, '../../Conformance/numeric-source-regressions.json'), 'utf8')) as { operands: { text: string; success: boolean; numeric?: boolean }[] };
const model = (body: string): string => `module M\n  feature F\n    slice StateChange S\n      command C\n        ${body}\n`;

describe('when protecting Phase A source boundaries', () => {
    it.each(['```embedded', '``` // not a closer', '````', '```custom'])('should preserve raw fenced Legacy text containing %s', embedded => {
        const body = `var text = """\n${embedded}\nnumbers exact\n""";`;
        const parsed = parse(model(`handler\n          \`\`\`csharp\n${body}\n          \`\`\``));
        parsed.success.should.equal(true);
        parsed.value.modules[0].features[0].slices[0].commands[0].handler!.code!.code.should.equal(body);
        parsed.value.sourceOptions!.numericMode.should.equal('legacy');
        JSON.stringify(toSyntaxJson(parsed.value)).should.not.contain('sourceOptions');
    });
    it('should use registered fence ownership in the numeric prepass and route candidate probe', () => {
        const source = 'numbers exact\npolicy P\n  ```custom\n```embedded\neventsource Foreign\n  stream Bad\nnumbers exact\n  ```\neventsource Orders\n  stream Changes\n' + model('stream Orders.Changes\n        produces Added\n          amount = 9007199254740993');
        const parsed = parseWithLanguages(source, new Set(['custom']));
        parsed.success.should.equal(true);
        parsed.value.eventSources!.map(source => source.name).should.deep.equal(['Orders']);
        const command = parsed.value.modules[0].features[0].slices[0].commands[0];
        command.stream!.eventSource.should.equal('Orders');
        JSON.stringify(toSyntaxJson(parsed.value)).should.contain('9007199254740993');
        const folder = compileApplication(new Map([['root.play', 'import "child.play"\n'], ['child.play', source]]), ['root.play'], new Set(['custom']));
        folder.success.should.equal(true);
        folder.value.sourceOptions!.numericMode.should.equal('exact');
        folder.value.eventSources!.length.should.equal(1);
    });
    it.each(vectors.operands)('should consume the complete Exact condition and policy operand $text', vector => {
        const parsed = parse('numbers exact\n' + model(`produces when amount == ${vector.text}\n          Added`));
        const policy = parse(`numbers exact\npolicy P\n  require claim "limit" matches ${vector.text}\n`);
        parsed.success.should.equal(vector.success);
        policy.success.should.equal(vector.success);
        if (vector.success) {
            const comparison = parsed.value.modules[0].features[0].slices[0].commands[0].produces[0].when as ComparisonConditionSyntax;
            const claim = policy.value.policies![0].condition as ClaimConditionSyntax;
            (comparison.right.kind === 'LiteralExpressionSyntax').should.equal(vector.numeric);
            (claim.matches!.kind === 'LiteralExpressionSyntax').should.equal(vector.numeric);
        }
    });
    it.each(['numbers legacy\nconcept B : Decimal\n', 'numbers exact\nconcept B : Decimal\n', 'numbers exact\nimport "a.play"\n'])('should retain invalid merged consensus in both file orders: %s', source => {
        for (const documents of [[parse('concept A : Decimal\n', 'a.play'), parse(source, 'b.play')], [parse(source, 'b.play'), parse('concept A : Decimal\n', 'a.play')]]) {
            const merged = mergeDocuments(documents);
            merged.success.should.equal(false);
            expect(() => toSyntaxJson(merged.value)).toThrow();
            expect(merged.value.concepts.find(concept => concept.name === 'A')!.location.path).toBe('a.play');
        }
    });
    it.each([
        ['capture C\n  numbers exact\n', 'PLAY0079'],
        ['capture C\n  nested item\n    numbers exact\n', 'PLAY0091'],
        ['capture C\n  map\n    bad line\n', 'PLAY0080'],
        ['capture C\n  append invalid\n', 'PLAY0084'],
        ['capture C\n  children invalid\n', 'PLAY0065'],
        ['capture C\n  nested invalid value\n', 'PLAY0066'],
        ['capture C\n  map\n    split invalid\n', 'PLAY0082'],
        ['capture C\n  map\n    split name by ","\n      target!\n', 'PLAY0083'],
        ['capture C\n  map\n    name = source translate\n      invalid\n', 'PLAY0081'],
        ['capture C\n  append Added\n    bad line\n', 'PLAY0085'],
        ['capture C\n  append Added\n    when\n', 'PLAY0086'],
        ['capture C\n  append Added\n    when amount from 1\n', 'PLAY0088'],
        ['capture C\n  append Added\n    when amount or\n', 'PLAY0090'],
        ['capture C\n  append Added\n    when amount or other and third\n', 'PLAY0089'],
        ['capture C\n  append Added\n    when amount +\n', 'PLAY0087'],
        ['capture C\n  append Added\n    when `unterminated\n', 'PLAY0156'],
        ['capture C\n  append Added\n    when added\n      bad line\n', 'PLAY0044'],
        ['capture C\n  map\n    amount = `valid` trailing garbage\n', 'PLAY0156'],
    ])('should diagnose malformed Exact capture data without changing Legacy skips: %s', (source, code) => {
        parseCaptureSource(source).diagnostics.should.deep.equal([]);
        const parsed = parseCaptureSource('numbers exact\n' + source);
        parsed.success.should.equal(false);
        parsed.diagnostics.map(diagnostic => diagnostic.code).should.contain(code);
    });
    it.each(['`open', '`value ${missing`', 'literal nope', 'not an expression'])('should diagnose malformed Exact projection expressions: %s', text => {
        parseProjectionSource(`projection P\n  from E\n    amount = ${text}\n`).success.should.equal(true);
        parseProjectionSource(`numbers exact\nprojection P\n  from E\n    amount = ${text}\n`).success.should.equal(false);
    });
    it.each([
        ['-0', 0], ['9007199254740993', 9007199254740992], ['100000000000000020', 100000000000000016], ['1.00000000000000000000000000001', 1], ['0.00000000000000000000000000001', 1e-29],
    ] as const)('should preserve source-backed Legacy Double values for %s', (token, expected) => {
        const parsed = parse(model(`produces Added\n          amount = ${token}`));
        parsed.success.should.equal(true);
        const value = parsed.value.modules[0].features[0].slices[0].commands[0].produces[0].mappings[0].source;
        if (value.kind !== 'LiteralExpressionSyntax') throw new Error('Expected a literal');
        expect(value.value).toBe(expected === 0 && token === '-0' ? -0 : expected);
        if (token === '-0') Object.is(value.value, -0).should.equal(true);
        JSON.stringify(toSyntaxJson(parsed.value)).should.not.contain('sourceOptions');
    });
    it('should preserve invalid consensus for import barrels and native placements in either traversal order', () => {
        for (const first of ['a.play', 'b.play']) {
            const files = new Map([
                ['root.play', `module M\n  feature F\n    import "${first}"\n    import "${first === 'a.play' ? 'b.play' : 'a.play'}"\n`],
                ['a.play', 'numbers exact\nconcept A : Decimal\n'],
                ['b.play', 'numbers legacy\nconcept B : Decimal\n'],
            ]);
            const merged = compileApplication(files, ['root.play']);
            merged.success.should.equal(false);
            expect(() => toSyntaxJson(merged.value)).toThrow();
            expect(merged.value.concepts.find(concept => concept.name === 'A')!.location.path).toBe('a.play');
        }
    });
    it('should preserve Unicode paths and complete capture transition spellings in Exact only', () => {
        const projection = parseProjectionSource('numbers exact\nprojection P\n  from E\n    amount = café.amount\n');
        projection.success.should.equal(true);
        const capture = parseCaptureSource('numbers exact\ncapture C\n  append Added\n    when amount from -1e-28 to 9007199254740993\n');
        capture.success.should.equal(true);
        expect(capture.value[0].appends![0].when!.fromValue).toBe('-1e-28');
        expect(capture.value[0].appends![0].when!.toValue).toBe('9007199254740993');
    });
    it('should retain keyword-named mappings inside their owning capture grammar', () => {
        const parsed = parseCaptureSource('numbers exact\ncapture C\n  map\n    numbers = 1\n    nested = 2\n  append Added\n    numbers = 3\n');
        parsed.success.should.equal(true);
        parsed.value[0].map!.length.should.equal(2);
        parsed.value[0].appends[0].mappings![0].property.should.equal('numbers');
    });
});
