// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseCaptureSource, parseProjectionSource, parseSpecificationSource, parseWithLanguages } from '../../ScreenplayCompiler';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const projection = (): Record<string, unknown> => JSON.parse(JSON.stringify(toSyntaxJson(parseProjectionSource('numbers exact\nprojection P\n  from E\n    amount = 1\n').value[0]))) as Record<string, unknown>;
const restored = (value: object): SyntaxNode => decodeExactSyntaxJson(JSON.stringify(value));
const seed = (body: string): string => `seed\n  for "global"\n    Added\n      ${body}\n`;

describe('when closing Exact source contracts', () => {
    it.each([
        (value: Record<string, unknown>) => { delete ((value.blocks as { mappings: Record<string, unknown>[] }[])[0].mappings[0]).source; },
        (value: Record<string, unknown>) => { (value.blocks as { mappings: Record<string, unknown>[] }[])[0].mappings[0].source = { kind: 'SeedSyntax', groups: [] }; },
        (value: Record<string, unknown>) => { (value.blocks as { mappings: Record<string, unknown>[] }[])[0].mappings[0].source = { kind: 'TypeRefSyntax', name: 'Decimal', isOptional: false, isCollection: false }; },
        (value: Record<string, unknown>) => { value.autoMap = 'invalid'; },
        (value: Record<string, unknown>) => { value.name = false; },
    ])('should reject invalid typed members on both sides of Exact transport', change => {
        const value = projection();
        const tree = restored(value);
        change(value);
        change(tree as unknown as Record<string, unknown>);
        expect(() => restored(value)).toThrow(InvalidSyntaxJson);
        expect(() => toSyntaxJson(tree)).toThrow(InvalidSyntaxJson);
    });
    it('should ignore nontransport AST metadata, retain defaults and normalize only nullable collections', () => {
        const tree = restored(projection()) as unknown as Record<string, unknown>;
        tree.extraMetadata = { kind: 'NotSyntax', number: 42 };
        const wire = JSON.stringify(toSyntaxJson(tree as unknown as SyntaxNode));
        expect(wire).not.toContain('extraMetadata');
        expect(() => decodeExactSyntaxJson(wire)).not.toThrow();
    });
    it.each([
        ['seed wrong\n', 'PLAY0128'],
        ['seed\n  wrong\n', 'PLAY0129'],
        ['seed\n  for "global"\n    wrong\n', 'PLAY0130'],
        [seed('amount ='), 'PLAY0131'],
        [seed('amount\n        numbers exact'), 'PLAY0131'],
        ['policy P\n  numbers exact\n', 'PLAY0113'],
        ['policy wrong-name\n  require authenticated\n', 'PLAY0112'],
        ['policy P\n', 'PLAY0114'],
        ['policy P\n  require authenticated\n  require authenticated\n', 'PLAY0441'],
        ['policy P\n  require authenticated\n  file p.cs\n', 'PLAY0440'],
        ['policy P\n  require role\n', 'PLAY0118'],
        ['policy P\n  require claim\n', 'PLAY0119'],
        ['policy P\n  require claim "limit"\n', 'PLAY0120'],
        ['policy P\n  require claim "limit" matches\n', 'PLAY0121'],
        ['policy P\n  file a.cs\n  file b.cs\n', 'PLAY0113'],
        ['policy P\n  file a.cs\n  ```csharp\ncode\n  ```\n', 'PLAY0113'],
    ])('should report native rejection %s without changing Legacy opaque diagnostics', (body, code) => {
        const exact = parse(`numbers exact\n${body}`, 'input.play');
        expect(exact.success).toBe(false);
        expect(exact.diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
        expect(parse(body).success).toBe(true);
        const files = new Map([['root.play', 'import "child.play"\n'], ['child.play', `numbers exact\n${body}`]]);
        expect(compileApplication(files, ['root.play']).success).toBe(false);
    });
    it.each([['projection', parseProjectionSource, 'PLAY0055'], ['capture', parseCaptureSource, 'PLAY0077'], ['specification', parseSpecificationSource, 'PLAY0093']] as const)('should require a declaration in an empty Exact %s document', (_family, read, code) => {
        const result = read('numbers exact\n# comment\n', 'input.play');
        expect(result.value).toEqual([]);
        expect(result.success).toBe(false);
        expect(result.diagnostics).toMatchObject([{ code, location: { line: 1, column: 1, path: 'input.play' } }]);
        expect(read('').success).toBe(true);
    });
    it.each(['', 'numbers exact\n'])('should preserve Unicode identifiers in newly modeled fields in mode %s', prefix => {
        const condition = parse(`${prefix}module M\n  feature F\n    slice StateChange S\n      command C\n        produces when café == 1\n          Added\n`);
        const production = condition.value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(production.when).toMatchObject({ kind: 'ComparisonConditionSyntax', left: 'café' });
        const capture = parseCaptureSource(`${prefix}capture C\n  map\n    result = café\n    split café by ","\n      café.name\n    translated = café translate\n      "yes" => café\n  append Added\n    when café from café to café\n`);
        expect(capture.success).toBe(true);
        expect(capture.value[0].map).toMatchObject([
            { source: { kind: 'PathExpressionSyntax', path: 'café' } },
            { source: { kind: 'PathExpressionSyntax', path: 'café' }, targets: ['café.name'] },
            { translations: [{ to: 'café' }] },
        ]);
        expect(capture.value[0].appends[0].when).toMatchObject({ properties: ['café'], fromValue: 'café', toValue: 'café' });
    });
    it.each([
        ['capture C\n  map\n    value = café𐐀\n', 'PLAY0152'],
        ['capture C\n  map\n    split café by ","\n      café𐐀\n', 'PLAY0083'],
        ['capture C\n  map\n    value = café translate\n      "x" => café𐐀\n', 'PLAY0081'],
    ])('should consume the full unsupported identifier rather than a valid prefix', (body, code) => {
        const result = parseCaptureSource(`numbers exact\n${body}`);
        expect(result.success).toBe(false);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
    it('should refuse supplementary UTF-16 word characters in the newly modeled seed grammar', () => {
        const result = parse('numbers exact\nseed\n  for "global"\n    Add𐐀\n      amount = 1\n');
        expect(result.success).toBe(false);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0130');
    });
    it.each([['screen', true], ['handler', true], ['screen', false], ['handler', false]] as const)('should consume custom %s code with legacy form %s before physical import discovery', (owner, legacyForm) => {
        const declaration = owner === 'screen' ? 'screen S' : 'command C\n        handler';
        const indent = owner === 'screen' ? '        ' : '          ';
        const open = legacyForm ? `${indent}custom\n${indent}  \`\`\`custom` : `${indent}\`\`\`custom`;
        const source = `numbers exact\nmodule M\n  feature F\n    slice StateChange S\n      ${declaration}\n${open}\nimport "fake.play"\nnumbers exact\n\`\`\` not a closing fence\n${indent}  \`\`\`\n`;
        const registry = new Set(['custom']);
        const parsed = parseWithLanguages(source, registry);
        expect(parsed.success).toBe(true);
        const compiled = compileApplication(new Map([['root.play', source]]), ['root.play'], registry);
        expect(compiled.success).toBe(true);
        expect(compiled.documents.map(document => document.path)).toEqual(['root.play']);
        expect(compiled.diagnostics.map(diagnostic => diagnostic.code)).not.toContain('PLAY0512');
    });
});
