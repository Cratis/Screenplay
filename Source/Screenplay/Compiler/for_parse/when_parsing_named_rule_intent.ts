// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ValidationRuleSyntax } from '../Syntax/Commands';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const serializeRule = (rule: ValidationRuleSyntax) => toSyntaxJson(rule);
const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check severity warning message "Invalid"\n';
const suffix = '\n          label not empty\n        hint String\n        implementation String\n      event Next\n        label String';
const rule = (source: string) => {
    const block = parse(source).value.modules[0].features[0].slices[0].commands[0].validations[0];
    if (block.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative rules');
    return block.rules[0];
};
const vectors: { name: string; authored: string; decoded: string; blank: boolean }[] = JSON.parse(readFileSync(new URL('../Conformance/handler-hint-whitespace.json', import.meta.url), 'utf8'));

describe('when parsing command named-rule intent', () => {
    it.each(['csharp', 'typescript', 'react', 'html', 'sql'])('should preserve every validation member and opaque %s code', language => {
        const body = `\`\`\`${language}\nreturn value => value != "hint";\n// command Fake\n\`\`\``;
        const direct = rule(prefix + body.split('\n').map(line => '            ' + line).join('\n'));
        const wrapped = rule(prefix + '            implementation\n              hint "Keep"\n' + body.split('\n').map(line => '              ' + line).join('\n'));
        expect(serializeRule({ ...wrapped, implementation: null })).toEqual(toSyntaxJson(direct));
        expect(wrapped.code?.code).toBe('return value => value != "hint";\n// command Fake');
        expect(wrapped.implementation?.hints.map(hint => hint.text)).toEqual(['Keep']);
        expect(Object.keys(toSyntaxJson(wrapped)!)).toEqual(['kind', 'code', 'file', 'implementation', 'message', 'property', 'rule', 'severity', 'value']);
    });
    it('should retain nonnull file and decoded ordered hints', () => {
        const parsed = rule(prefix + '            implementation\n              hint " first "\n              hint "Keep \\"quoted\\" criteria"\n              file Rules/Check.cs');
        expect(parsed.file?.path).toBe('Rules/Check.cs');
        expect(parsed.code).toBeNull();
        expect(parsed.value).toMatchObject({ kind: 'PathExpressionSyntax', path: 'Check' });
        expect(parsed.property).toBe('label');
        expect(parsed.rule).toBe('Rule');
        expect(parsed.message).toBe('Invalid');
        expect(parsed.severity).toBe('Warning');
        expect(parsed.implementation?.hints.map(hint => hint.text)).toEqual([' first ', 'Keep "quoted" criteria']);
    });
    it.each(vectors)('should share hint whitespace vector $name', ({ authored, decoded, blank }) => {
        const source = prefix + `            implementation\n              hint "${authored}"`;
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toEqual(blank ? ['PLAY0493'] : []);
        if (!blank) expect(rule(source).implementation?.hints[0].text).toBe(decoded);
    });
    it.each([
        ['implementation\n              hint " "', 'PLAY0493'],
        ['implementation\n              hint "Keep"\n                file A.cs', 'PLAY0493'],
        ['implementation\n              file A.cs\n              file B.cs', 'PLAY0494'],
        ['implementation\n            implementation', 'PLAY0492'],
        ['file A.cs\n            implementation', 'PLAY0494'],
        ['implementation\n            file A.cs', 'PLAY0494'],
        ['implementation\n              implementation', 'PLAY0492'],
        ['unknown\n              nested', 'PLAY0144'],
        ['csharp', 'PLAY0163'],
        ['implementation\n            ```csharp\n            return true;\n            ```', 'PLAY0494'],
    ])('should reject %s and retain sibling rules and declarations', (body, code) => {
        const parsed = parse(prefix + '            ' + body + suffix);
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
        const slice = parsed.value.modules[0].features[0].slices[0];
        expect(slice.events[0].name).toBe('Next');
        expect(slice.commands[0].properties.map(property => property.name)).toEqual(['label', 'hint', 'implementation']);
        const validation = slice.commands[0].validations[0];
        expect(validation.kind === 'DeclarativeValidateSyntax' && validation.rules.length).toBe(2);
    });
    it('should visit each payload and hint once at actual source lines', () => {
        class Walker extends ScreenplaySyntaxWalker {
            nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const parsed = parse(prefix + '            implementation\n              hint "Keep"\n              file A.cs');
        const walker = new Walker();
        walker.visitApplication(parsed.value);
        expect(walker.nodes.filter(node => node.kind === 'ImplementationHintSyntax').map(node => node.location.line)).toEqual([9]);
        expect(walker.nodes.filter(node => node.kind === 'FileReferenceSyntax').map(node => node.location.line)).toEqual([10]);
        const fenced = parse(prefix + '            implementation\n              ```csharp\n              return true;\n              ```');
        walker.visitApplication(fenced.value);
        expect(walker.nodes.filter(node => node.kind === 'CodeBlockSyntax')).toHaveLength(1);
    });
    it('should keep bare legacy named rules unchanged without inventing a payload', () => {
        const source = prefix + suffix;
        expect(parse(source).diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
        expect(rule(source)).toMatchObject({ rule: 'Rule', file: null, code: null, implementation: null });
        expect(rule(source).value).toMatchObject({ kind: 'PathExpressionSyntax', path: 'Check' });
    });
    it('should retain the direct legacy language-line form', () => {
        const source = prefix + '            csharp\n              ```\n              return true;\n              ```';
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0397']);
        expect(rule(source).code?.code).toBe('return true;');
        expect(rule(source).implementation).toBeNull();
    });
    it('should reject malformed programmatic wrapped rules when serialized', () => {
        const parsed = rule(prefix + '            implementation\n              file A.cs');
        expect(() => serializeRule({ ...parsed, rule: 'NotEmpty' })).toThrow();
        expect(() => serializeRule({ ...parsed, value: null })).toThrow();
        expect(() => serializeRule({ ...parsed, value: { kind: 'PathExpressionSyntax', path: 'Invalid.Name', location: parsed.location } })).toThrow();
        expect(() => serializeRule({ ...parsed, value: { kind: 'PathExpressionSyntax', path: 'Check\n', location: parsed.location } })).toThrow();
        expect(() => serializeRule({ ...parsed, value: { kind: 'PathExpressionSyntax', path: 'Check\u{10400}', location: parsed.location } })).toThrow();
        expect(() => serializeRule({ ...parsed, value: { kind: 'PathExpressionSyntax', path: 'Check\u00e9', location: parsed.location } })).not.toThrow();
        expect(() => serializeRule({ ...parsed, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'true', location: parsed.location } })).toThrow();
        expect(() => serializeRule({ ...parsed, implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text: ' ', location: parsed.location }], location: parsed.location } })).toThrow();
    });
    it('should reject a supplementary-plane rule name when parsing', () => {
        const parsed = parse(prefix.replace('Check', 'Check\u{10400}'));
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0143');
    });
    it('should reject an implementation wrapper in a concept validation', () => {
        const parsed = parse('concept Label : String\n  validate\n    rule Check\n      implementation\n        hint "Keep"');
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0144']);
        expect(parsed.diagnostics[0].location.line).toBe(4);
    });
    it.each([
        ['a bare rule', 'concept Label : String\n  validate\n    rule Check\n    not empty'],
        ['a file payload', 'concept Label : String\n  validate\n    rule Check\n      file A.cs'],
        ['a fenced payload', 'concept Label : String\n  validate\n    rule Check\n      ```csharp\n      return true;\n      ```'],
        ['a tagged payload', 'concept Label : String\n  validate\n    rule Check\n      csharp\n        ```\n        return true;\n        ```'],
        ['a fence among rules', 'concept Label : String\n  validate\n    not empty\n    ```csharp\n    return true;\n    ```'],
    ])('should accept %s in a concept validation', (_name, source) => {
        expect(parse(source).diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
    });
    it('should reject an unknown payload under a concept named rule', () => {
        const parsed = parse('concept Label : String\n  validate\n    rule Check\n      bogus\n        deeper');
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0144']);
    });
    it('should reject a wrapper after a builtin rule and the payload nested under it', () => {
        const parsed = parse('concept Label : String\n  validate\n    not empty\n      implementation\n        hint "Keep"\n    rule Check');
        expect(parsed.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)).toEqual(['PLAY0141@4', 'PLAY0141@5']);
    });
});
