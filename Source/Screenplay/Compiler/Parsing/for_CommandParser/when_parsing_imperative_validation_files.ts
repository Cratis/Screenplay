// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toCompleteSyntaxJson } from '../../Syntax/SyntaxJson';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { FileValidateSyntax, DeclarativeValidateSyntax } from '../../Syntax/Commands';

const cases = [
    ['concept Money : Decimal\n  validate\n    file Rules/Money.cs', 'Rules/Money.cs'],
    ['module M\n  feature F\n    slice StateChange S\n      command Pay\n        validate\n          file Rules/Pay.cs', 'Rules/Pay.cs'],
    ['module M\n  feature F\n    slice StateChange S\n      command Pay\n        validate\n          file Pay file.cs', 'Pay file.cs'],
];

describe('when parsing imperative validation files', () => {
    it.each(cases)('should preserve and visit the validation file for %s', (source, path) => {
        const parsed = parse(source);
        expect(parsed.diagnostics).toEqual([]);
        const json = toCompleteSyntaxJson(parsed.value!);
        expect(JSON.stringify(json)).toContain('"kind":"FileValidateSyntax"');
        expect(JSON.stringify(json)).toContain(`"path":"${path}"`);
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(parsed.value!);
        expect(kinds).toContain('FileReferenceSyntax');
    });

    it.each(['file not empty', 'file min 1.5', 'file matches "a/b.cs"', 'file not empty\tmessage "Required"', 'file not empty\n          other not empty'])('should keep a declarative rule on a property named file: %s', body => {
        const parsed = parse(command(body));
        expect(parsed.diagnostics).toEqual([]);
        const validation = parsed.value!.modules[0].features[0].slices[0].commands[0].validations[0] as DeclarativeValidateSyntax;
        expect(validation.kind).toBe('DeclarativeValidateSyntax');
        expect(validation.rules[0].property).toBe('file');
    });

    it.each(['Rules/Money.cs', 'rules/Money.cs', 'minimum.cs', 'contains/X.cs'])('should treat a path that starts with an operator word as an include: %s', path => {
        const parsed = parse(command('file ' + path));
        expect(parsed.diagnostics).toEqual([]);
        const validation = parsed.value!.modules[0].features[0].slices[0].commands[0].validations[0] as FileValidateSyntax;
        expect(validation.kind).toBe('FileValidateSyntax');
        expect(validation.file.path).toBe(path);
    });

    it.each(['file max 10', 'file min 3', 'file rule Positive', 'file >= 1'])('should keep an operator after the file property as a rule: %s', body => {
        const parsed = parse(command(body));
        expect(parsed.diagnostics).toEqual([]);
        const validation = parsed.value!.modules[0].features[0].slices[0].commands[0].validations[0] as DeclarativeValidateSyntax;
        expect(validation.kind).toBe('DeclarativeValidateSyntax');
        expect(validation.rules[0].property).toBe('file');
    });

    it('should not claim a file include when another child follows', () => {
        const parsed = parse(command('file Rules/Pay.cs\n          other not empty'));
        const validation = parsed.value!.modules[0].features[0].slices[0].commands[0].validations[0] as DeclarativeValidateSyntax;
        expect(validation.kind).toBe('DeclarativeValidateSyntax');
        expect(validation.rules[0].property).toBe('other');
        expect(parsed.diagnostics.length).toBeGreaterThan(0);
    });

    it('should leave the next command directive outside the include', () => {
        const parsed = parse(command('file Rules/Pay.cs\n        handler\n          file Handlers/Pay.cs'));
        expect(parsed.diagnostics).toEqual([]);
        const declaration = parsed.value!.modules[0].features[0].slices[0].commands[0];
        expect((declaration.validations[0] as FileValidateSyntax).file.path).toBe('Rules/Pay.cs');
        expect(declaration.handler!.file!.path).toBe('Handlers/Pay.cs');
    });
});

const command = (body: string): string =>
    'module M\n  feature F\n    slice StateChange S\n      command Pay\n        file String\n        other String\n        validate\n          ' + body;
