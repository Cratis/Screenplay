// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';

const source = (body: string) => `concept OrderId : String\n  generate rule Order\n    ${body}`;

describe('when reading generation rule bodies', () => {
    it('should preserve the direct file body separately from concept provenance', () => {
        const result = parse(source('file Generators/Order.cs').replace('  generate', '  file Provenance.cs\n  generate'));
        result.diagnostics.should.be.empty;
        // Existing TS provenance projection remains unchanged; only the nested source is executable input.
        Object.hasOwn(result.value!.concepts[0], 'file').should.equal(false);
        result.value!.concepts[0].generation!.file!.path.should.equal('Generators/Order.cs');
    });
    it('should preserve a tagged inline body and visit its code', () => {
        const result = parse(source('```csharp\n    return value;\n    ```'));
        result.diagnostics.should.be.empty;
        result.value!.concepts[0].generation!.code!.code.should.equal('return value;');
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(result.value!);
        kinds.should.include('GenerationSyntax');
        kinds.should.include('CodeBlockSyntax');
    });
    it('should visit the nested implementation file', () => {
        const kinds: string[] = [];
        class Walker extends ScreenplaySyntaxWalker { visitNode(node: SyntaxNode): void { kinds.push(node.kind); } }
        new Walker().visitApplication(parse(source('file Order.cs')).value!);
        kinds.should.include('FileReferenceSyntax');
    });
    for (const [body, code] of [
        ['file First.cs\n    file Second.cs', DiagnosticCodes.ConflictingImplementationSources],
        ['file First.cs\n    ```csharp\n    return value;\n    ```', DiagnosticCodes.ConflictingImplementationSources],
        ['implementation\n      file Order.cs', DiagnosticCodes.UnknownRuleImplementationDirective],
        ['file Order.cs\n      unexpected', DiagnosticCodes.UnknownRuleImplementationDirective],
        ['csharp\n      ```csharp\n      return value;\n      ```', DiagnosticCodes.UnknownRuleImplementationDirective],
        ['```\n    value\n    ```', DiagnosticCodes.ExpectedCodeFence]
    ]) it(`should reject ${JSON.stringify(body)}`, () => {
        parse(source(body)).diagnostics.some(diagnostic => diagnostic.code === code).should.equal(true);
    });
    it('should not treat a builtin child as concept provenance', () => {
        const result = parse(source('file Wrong.cs').replace('rule Order', 'uuid'));
        result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.UnknownRuleImplementationDirective).should.equal(true);
        Object.hasOwn(result.value!.concepts[0], 'file').should.equal(false);
    });
    it('should refuse conflicting programmatic payloads', () => {
        const location = sourceLocation(1, 1);
        (() => toSyntaxJson({ kind: 'GenerationSyntax', strategy: 'rule', rule: 'Order', location,
            file: { kind: 'FileReferenceSyntax', path: 'Order.cs', location }, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'return value;', location }
        } as unknown as SyntaxNode)).should.throw('exactly one');
    });
    it('should refuse a programmatic body on a builtin', () => {
        const location = sourceLocation(1, 1);
        (() => toSyntaxJson({ kind: 'GenerationSyntax', strategy: 'uuid', location, file: { kind: 'FileReferenceSyntax', path: 'Order.cs', location } } as unknown as SyntaxNode)).should.throw('named generation rule');
    });
});
