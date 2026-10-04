// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { discoverImports, parse, parseCaptureSource, parseProjectionSource, parseSpecificationSource, parseWithLanguages } from '../../ScreenplayCompiler';
import { mergeDocuments } from '../../Files/PlayFolderMerge';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import { ExpressionSyntax } from '../../Syntax/Expressions';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';

const source = readFileSync(resolve(__dirname, '../../Conformance/exact-numbers.play'), 'utf8');
class Numbers extends ScreenplaySyntaxWalker {
    readonly values: string[] = [];
    override visitExpression(node: ExpressionSyntax): void {
        if (node.kind === 'LiteralExpressionSyntax' && typeof node.value === 'object' && node.value !== null) this.values.push(node.value.value);
        super.visitExpression(node);
    }
}
const model = (token: string): string => `module M\n  feature F\n    slice StateChange S\n      command C\n        produces E\n          amount = ${token}\n`;

describe('when reading exact source documents', () => {
    it('should match every restored member of the complete native Exact fixture, including Unicode names', () => {
        const golden = readFileSync(resolve(__dirname, '../../Conformance/exact-numbers.syntax.json'), 'utf8');
        const sourceWire = JSON.stringify(toSyntaxJson(parse(source).value));
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(sourceWire))).should.equal(JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(golden))));
    });
    it('should retain exact values in every shared numeric-bearing family', () => {
        const parsed = parse(source);
        parsed.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
        const numbers = new Numbers();
        numbers.visitApplication(parsed.value);
        numbers.values.length.should.be.greaterThan(40);
        numbers.values.should.include('9007199254740993');
        numbers.values.should.include('9007199254740994');
        numbers.values.should.include('9223372036854775808');
        numbers.values.should.include('0.0000000000000000000000000001');
        numbers.values.should.include('0');
        const wire = JSON.stringify(toSyntaxJson(parsed.value));
        const restored = decodeExactSyntaxJson(wire);
        const rendered = JSON.stringify(toSyntaxJson(restored));
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(rendered))).should.equal(rendered);
        wire.should.not.contain('9007199254740992');
    });

    it.each(['1e-29', '1e29', '1e308', '79228162514264337593543950336', '1.00000000000000000000000000001'])('should refuse a complete out-of-domain operand %s without successful opaque fallback', token => {
        const parsed = parse(`numbers exact\n${model(token)}`);
        parsed.success.should.equal(false);
        parsed.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0511').should.equal(true);
        expect(() => toSyntaxJson(parsed.value)).toThrow(InvalidSyntaxJson);
        const nested = parse(`numbers exact\n${model(`{"items":[${token}]}`)}`);
        nested.success.should.equal(false);
        nested.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0511').should.equal(true);
        expect(() => toSyntaxJson(nested.value)).toThrow(InvalidSyntaxJson);
    });

    it.each(['numbers legacy\n', 'numbers exact\nnumbers exact\n', 'domain Billing\nnumbers exact\n', 'import Other.Amount\nnumbers exact\n'])('should retain failed numeric assertions instead of defaulting to Legacy', prefix => {
        const parsed = parse(prefix + model('1'), 'input.play');
        parsed.success.should.equal(false);
        expect(() => toSyntaxJson(parsed.value)).toThrow(InvalidSyntaxJson);
    });

    it('should establish options for every standalone root and before import discovery', () => {
        const projection = parseProjectionSource('\uFEFF# comment\nnumbers exact\nprojection P\n  from E\n    amount = 9007199254740993\n');
        const capture = parseCaptureSource('numbers exact\ncapture C\n  append E\n    amount = 9007199254740993\n');
        const specification = parseSpecificationSource('numbers exact\nspecification S\n  when C\n    amount = 9007199254740993\n');
        for (const result of [projection, capture, specification]) {
            result.success.should.equal(true);
            const wire = JSON.stringify(toSyntaxJson(result.value[0]));
            JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(wire))).should.contain('9007199254740993');
        }
        discoverImports('numbers exact\nimport "child.play"\n').map(imported => imported.fileImport.pattern).should.deep.equal(['child.play']);
    });

    it('should preserve caller-registered fences without reading their text as numbers', () => {
        const parsed = parseWithLanguages('numbers exact\npolicy P\n  ```custom\nnumbers exact\n9007199254740993\n  ```\n', new Set(['custom']));
        parsed.success.should.equal(true);
        parsed.value.policies![0].code!.language.should.equal('custom');
        parsed.value.policies![0].code!.code.should.equal('numbers exact\n9007199254740993');
    });

    it('should distinguish physical declaration modes without inheriting marked barrels', () => {
        mergeDocuments([parse('numbers exact\nimport "child.play"\n'), parse(model('9007199254740993'), 'child.play')]).success.should.equal(false);
        mergeDocuments([parse('import "child.play"\n'), parse(`numbers exact\n${model('9007199254740993')}`, 'child.play')]).success.should.equal(true);
        const placed = parse('module M\n', 'module.play', ['M']);
        mergeDocuments([parse(`numbers exact\n${model('1')}`), placed]).success.should.equal(false);
    });

    it('should carry the caller registry through import discovery and folder parsing', () => {
        const files = new Map([
            ['root.play', 'import "module.play"\n'],
            ['module.play', 'numbers exact\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          ```custom\nimport "fake.play"\nnumbers exact\n          ```\n'],
        ]);
        const parsed = compileApplication(files, ['root.play'], new Set(['custom']));
        parsed.success.should.equal(true);
        parsed.documents.map(document => document.path).should.deep.equal(['root.play', 'module.play']);
        parsed.value.sourceOptions!.numericMode.should.equal('exact');
    });

    it('should preserve quoted diamond imports without silently inheriting mode into a declaration', () => {
        const files = new Map([
            ['root.play', 'numbers exact\nimport "a.play"\nimport "b.play"\n'],
            ['a.play', 'import "child.play"\n'],
            ['b.play', 'import "child.play"\n'],
            ['child.play', `numbers exact\n${model('9007199254740993')}`],
        ]);
        const parsed = compileApplication(files, ['root.play']);
        parsed.success.should.equal(true);
        parsed.documents.length.should.equal(4);
        files.set('child.play', model('9007199254740993'));
        const mixed = compileApplication(files, ['root.play']);
        mixed.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0512').should.equal(true);
        mixed.success.should.equal(false);
    });

    it('should report the actual BOM-adjusted preamble position', () => {
        const parsed = parse('\uFEFFnumbers legacy\n', 'input.play');
        parsed.diagnostics[0].location.should.deep.equal({ line: 1, column: 2, path: 'input.play' });
    });

    it('should leave Legacy rounding and zero conversion untouched', () => {
        const legacy = JSON.stringify(toSyntaxJson(parse(model('9007199254740993')).value));
        const exact = JSON.stringify(toSyntaxJson(parse(`numbers exact\n${model('9007199254740993')}`).value));
        legacy.should.contain('9007199254740992');
        legacy.should.not.contain('sourceOptions');
        exact.should.contain('9007199254740993');
        exact.should.not.equal(legacy);
    });
});
