// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { RemoveWithSyntax } from '../Syntax/Projections';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const removal = (result: CompilationResult<ApplicationSyntax>) => result.value.modules[0].features[0].slices[0].projections[0].blocks[0] as RemoveWithSyntax;
const source = (key: string[]) => [
    'module Billing',
    '  feature Payments',
    '    slice StateView Totals',
    '      event PaymentRemoved',
    '        invoice String',
    '        method String',
    '      projection Totals',
    '        remove with PaymentRemoved',
    ...key.map(line => `          ${line}`),
].join('\n');

class key_sources extends ScreenplaySyntaxWalker {
    readonly nodes: string[] = [];
    visitNode(node: SyntaxNode): void { this.nodes.push(node.kind); }
}

describe('when parsing removal key parts', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let visited: string[];

    beforeEach(() => {
        result = parse(source(['key {', '  invoiceId = invoice', '  method = method', '}']));
        const walker = new key_sources();
        walker.visitApplication(result.value);
        visited = walker.nodes;
    });

    it('should admit the untyped child block', () => result.success.should.be.true);
    it('should keep the untyped key', () => removal(result).partsKey!.type.should.equal(''));
    it('should keep the parts in authored order', () => removal(result).partsKey!.parts.map(part => part.property).should.deep.equal(['invoiceId', 'method']));
    it('should walk both part sources', () => visited.filter(kind => kind === 'PathExpressionSyntax').should.have.lengthOf(2));
    it('should keep the scalar key absent', () => (removal(result).key === null).should.be.true);
});

describe('when repeating a removal key', () => {
    let result: CompilationResult<ApplicationSyntax>;
    beforeEach(() => { result = parse(source(['key literal "first"', 'key', '  invoiceId = invoice', '  method = method'])); });
    it('should report the conflicting key', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0662']));
});

describe('when parsing an expression removal key child', () => {
    let result: CompilationResult<ApplicationSyntax>;
    beforeEach(() => { result = parse(source(['key invoice'])); });
    it('should preserve the expression for key-shape validation', () => removal(result).key!.kind.should.equal('PathExpressionSyntax'));
});

describe('when parsing an unknown removal directive', () => {
    let result: CompilationResult<ApplicationSyntax>;
    beforeEach(() => { result = parse(source(['other value'])); });
    it('should reject the directive', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0069']));
});
