// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseProjectionSource, parseCaptureSource, parseSpecificationSource } from '../../ScreenplayCompiler';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { syntaxDefinitions } from '../../Syntax/SyntaxDefinitions';

const model = (body: string): string => `module M\n  feature F\n    slice StateChange S\n      ${body}\n`;
const restored = (): ApplicationSyntax => decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(parse('numbers exact\nconcept A : Decimal\n').value))) as ApplicationSyntax;

const projectionErrors = [
    ['from E\n    numbers exact', 'PLAY0075'],
    ['from E\n    bad line', 'PLAY0075'],
    ['every\n    bad line', 'PLAY0075'],
    ['all\n    clear with', 'PLAY0075'],
    ['from E\n    key Composite {\n      bad line\n    }', 'PLAY0072'],
    ['from E\n    key Composite {\n      }', 'PLAY0074'],
    ['from E\n    key Composite {\n      part = `value ${1}`\n    }', 'PLAY0073'],
] as const;

describe('when addressing the second Phase A review', () => {
    it.each(projectionErrors)('should report Exact mapping/key failure %s', (body, code) => {
        const source = `projection P\n  ${body}\n`;
        parseProjectionSource(source).success.should.equal(true);
        const result = parseProjectionSource('numbers exact\n' + source);
        result.success.should.equal(false);
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(code);
    });
    it('should keep mapping opcode words as legal properties rather than numeric directives', () => {
        const result = parseProjectionSource('numbers exact\nprojection P\n  from E\n    numbers = 1\n    nested = 2\n    increment = 3\n');
        result.success.should.equal(true);
        const block = result.value[0].blocks[0];
        if (block.kind !== 'FromSyntax') throw new Error('Expected from');
        block.mappings.map(mapping => mapping.property).should.deep.equal(['numbers', 'nested', 'increment']);
    });
    it.each(['', 'numbers exact\n'])('should preserve native Unicode names in newly modeled fields with %s', prefix => {
        const value = prefix === '' ? '1' : '9007199254740993';
        const seed = parse(`${prefix}seed\n  for "global"\n    Addéd\n      café = {"literalType":"ExactNumber","value":[${value}]}\n`).value.seeds![0];
        seed.groups[0].events[0].event.should.equal('Addéd');
        seed.groups[0].events[0].properties[0].property.should.equal('café');
        parse(`${prefix}policy Café\n  require authenticated\n`).value.policies![0].name.should.equal('Café');
        const app = parse(prefix + model(`reaction React\n        when Addéd\n          invokes Changé\n            @café = ${value}`));
        const invoke = app.value.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0];
        invoke.command.should.equal('Changé');
        invoke.mappings![0].property.should.equal('café');
        const spec = parseSpecificationSource(`${prefix}specification Test\n  given Addéd\n    café = ${value}\n  when Changé\n    café = ${value}\n  then Addéd\n    café = ${value}\n  then no readmodel Viéw for {"id":${value}}\n  then query ViéwBy\n    arguments\n      café = ${value}\n    result\n      café = ${value}\n  then operation Sénd\n    café = ${value}\n`).value[0];
        spec.given[0].values[0].property.should.equal('café');
        spec.when!.values[0].property.should.equal('café');
        spec.thenEvents[0].values[0].property.should.equal('café');
        spec.thenAbsentReadModels![0].name.should.equal('Viéw');
        spec.thenQueries![0].query.should.equal('ViéwBy');
        spec.thenOperations![0].values[0].property.should.equal('café');
        if (prefix !== '') JSON.stringify(toSyntaxJson(spec)).should.contain('9007199254740993');
    });
    it.each(['modules', 'concepts', 'imports', 'policies', 'eventSources'])('should refuse mandatory null %s at the Exact writer boundary', member => {
        const root = restored();
        Object.assign(root, { [member]: null });
        expect(() => toSyntaxJson(root)).toThrow(InvalidSyntaxJson);
    });
    it.each(['modules', 'concepts', 'imports', 'types', 'seeds', 'eventSources'])('should refuse null and wrong-kind %s items at the Exact writer boundary', member => {
        for (const item of [null, { kind: 'UnknownSyntax', location: { line: 1, column: 1 } }, { kind: 'LiteralExpressionSyntax', value: true, location: { line: 1, column: 1 } }]) {
            const root = restored();
            Object.assign(root, { [member]: [item] });
            expect(() => toSyntaxJson(root)).toThrow(InvalidSyntaxJson);
        }
    });
    it.each([
        () => parse('numbers exact\nconcept A : Decimal\n').value,
        () => parseProjectionSource('numbers exact\nprojection P\n  from E\n    amount = 1\n').value[0],
        () => parseCaptureSource('numbers exact\ncapture C\n  append E\n    amount = 1\n').value[0],
        () => parseSpecificationSource('numbers exact\nspecification T\n  when C\n    amount = 1\n').value[0],
    ])('should freeze every restored source-root option', factory => {
        const root = decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(factory()))) as SyntaxNode & { sourceOptions: object };
        Object.isFrozen(root.sourceOptions).should.equal(true);
        expect(() => Object.assign(root.sourceOptions, { numericMode: 'legacy' })).toThrow(TypeError);
        JSON.stringify(toSyntaxJson(root)).should.contain('"numericMode":"exact"');
    });
    it.each(['csharp', 'typescript', 'react', 'html', 'sql', 'custom'])('should ignore fake import/mode roots inside the %s barrel fence', language => {
        for (const prefix of ['', 'numbers exact\n']) {
            const files = new Map([
                ['root.play', 'import "barrel.play"\n'],
                ['barrel.play', `${prefix}policy P\n  \`\`\`${language}\nimport "fake.play"\nnumbers exact\n  \`\`\`\nimport "child.play"\n`],
                ['child.play', `${prefix}concept A : Decimal\n`],
                ['fake.play', 'numbers legacy\nconcept Fake : Decimal\n'],
            ]);
            const result = compileApplication(files, ['root.play'], language === 'custom' ? new Set(['custom']) : undefined);
            result.success.should.equal(true);
            result.documents.map(document => document.path).should.deep.equal(['root.play', 'barrel.play', 'child.play']);
            result.value.sourceOptions!.numericMode.should.equal(prefix === '' ? 'legacy' : 'exact');
        }
    });
    it.each([
        () => parse('numbers exact\nconcept A : Decimal\n').value,
        () => parseProjectionSource('numbers exact\nprojection P\n  from E\n    amount = 1\n').value[0],
        () => parseCaptureSource('numbers exact\ncapture C\n  append E\n    amount = 1\n').value[0],
        () => parseSpecificationSource('numbers exact\nspecification T\n  when C\n    amount = 1\n').value[0],
    ])('should share every native collection null/item contract between Exact writing and reading', factory => {
        const root = decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(factory())));
        const members = (syntaxDefinitions[root.kind as keyof typeof syntaxDefinitions] as unknown as { properties: Record<string, { type?: string; anyOf?: { type?: string }[] }> }).properties;
        for (const [name, member] of Object.entries(members)) {
            if (member.type !== 'array' && !member.anyOf?.some(alternative => alternative.type === 'array')) continue;
            const nullable = member.anyOf?.some(alternative => alternative.type === 'null') ?? false;
            const withNull = { ...root, [name]: null };
            if (nullable) expect(() => decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(withNull)))).not.toThrow();
            else expect(() => toSyntaxJson(withNull)).toThrow(InvalidSyntaxJson);
            for (const item of [null, { kind: 'UnknownSyntax' }, { kind: 'LiteralExpressionSyntax', value: true }]) expect(() => toSyntaxJson({ ...root, [name]: [item] })).toThrow(InvalidSyntaxJson);
        }
    });
    it('should freeze nested restored source roots before consensus', () => {
        const app = parse('numbers exact\n' + model('projection P\n        from E\n          amount = 1\n      capture C\n        append E\n          amount = 1\n      specification T\n        when C\n          amount = 1')).value;
        const root = decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(app))) as ApplicationSyntax;
        const slice = root.modules[0].features[0].slices[0];
        for (const node of [root, ...slice.projections, ...slice.captures, ...slice.specifications]) Object.isFrozen(node.sourceOptions).should.equal(true);
    });
});
