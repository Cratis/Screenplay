// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseSpecificationSource } from '../../ScreenplayCompiler';
import { mergeDocuments } from '../../Files/PlayFolderMerge';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const declarations = 'example Root : E\n  p = 1\nmodule M\n  example Module : E\n    p = 2\n  feature F\n    example Feature : E\n      p = 3\n    slice StateChange S\n      example Slice : E\n        p = 4';

describe('when parsing typed examples', () => {
    it('should retain examples in every declaration scope', () => {
        const result = parse(declarations);
        result.diagnostics.should.deep.equal([]);
        result.value.examples![0].name.should.equal('Root');
        const module = result.value.modules[0];
        module.examples![0].name.should.equal('Module');
        module.features[0].examples![0].name.should.equal('Feature');
        module.features[0].slices[0].examples![0].name.should.equal('Slice');
    });

    it('should retain the examples in a specification-only document', () => {
        const result = parseSpecificationSource('example AcmeInvoice : RegisterInvoice\n  total = 1000\nspecification S\n  when AcmeInvoice total = 5000');
        result.diagnostics.should.deep.equal([]);
        result.value[0].examples![0].type.should.equal('RegisterInvoice');
        result.value[0].when!.commandType.should.equal('AcmeInvoice');
        result.value[0].when!.inlineProperty!.should.equal('total');
        result.value[0].when!.values[0].source.kind.should.equal('LiteralExpressionSyntax');
    });

    it.each(['given', 'when', 'when append', 'then', 'given readmodel', 'then readmodel', 'then readmodel exactly'])('should accept a qualified reference in %s', keyword => {
        const prefix = keyword.endsWith(' exactly') ? keyword.slice(0, -8) : keyword;
        const suffix = keyword.endsWith(' exactly') ? ' exactly' : '';
        const result = parseSpecificationSource(`specification S\n  ${prefix} M.F.AcmeInvoice${suffix} lines = [{"quantity":2}]\n    total = 5000`);
        result.diagnostics.should.deep.equal([]);
        JSON.stringify(toSyntaxJson(result.value[0])).should.contain('"inlineProperty":"lines"');
    });

    it.each(['given', 'when', 'when append', 'then', 'given readmodel', 'then readmodel'])('should reject a duplicate header assignment in %s', keyword => {
        const result = parseSpecificationSource(`specification S\n  ${keyword} AcmeInvoice total = 1000\n    total = 5000`);
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0519@3']);
    });

    it.each(['example Invoice', 'example Invoice :', 'example Invoice : Foo with Bar'])('should reject %s', header => {
        parse(header).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0518']);
    });

    it('should reject two inline assignments', () => {
        parseSpecificationSource('specification S\n  when AcmeInvoice name = "Acme" total = 5000').success.should.equal(false);
    });

    it('should preserve every example while merging placed files', () => {
        const merged = mergeDocuments([
            parse('example Root : E\n  p = 1', 'root.play'),
            parse('example ModuleOne : E\n  p = 2', 'one.play', ['M']),
            parse('example ModuleTwo : E\n  p = 3', 'two.play', ['M']),
            parse('example FeatureOne : E\n  p = 4', 'feature-one.play', ['M', 'F']),
            parse('example FeatureTwo : E\n  p = 5', 'feature-two.play', ['M', 'F'])
        ]);
        merged.diagnostics.should.deep.equal([]);
        merged.value.examples!.length.should.equal(1);
        merged.value.modules[0].examples!.length.should.equal(2);
        merged.value.modules[0].features[0].examples!.length.should.equal(2);
    });

    it('should walk the examples and their values', () => {
        const nodes: SyntaxNode[] = [];
        class Walker extends ScreenplaySyntaxWalker {
            override visitNode(node: SyntaxNode): void { nodes.push(node); }
        }
        new Walker().visitApplication(parse(declarations).value);
        nodes.filter(node => node.kind === 'SpecificationExampleSyntax').length.should.equal(4);
        nodes.filter(node => node.kind === 'PropertyMappingSyntax').length.should.equal(4);
    });

    it.each(['', 'numbers exact\n'])('should carry example values on the syntax wire in %s mode', preamble => {
        const result = parse(preamble + declarations);
        result.diagnostics.should.deep.equal([]);
        JSON.stringify(toSyntaxJson(result.value)).should.contain('"kind":"SpecificationExampleSyntax"');
    });
});
