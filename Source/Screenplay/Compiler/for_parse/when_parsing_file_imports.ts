// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, discoverImports } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

const source = [
    'import Customers.CustomerRegistered',
    'import "**/*.play"',
    'concept OrderId : Uuid',
    'module Ordering',
    '  description "Orders"',
    '  import "Orders/*.play"',
    '  feature Orders',
    '    import "Slices/**/*.play"',
    '    feature Drafts',
    '      import "Drafts.play"',
];

describe('when parsing file imports', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(...source);
    });

    it('should succeed', () => {
        result.success.should.be.true;
    });

    it('should keep the qualified import an import of a name', () => {
        result.value.imports.map(each => each.qualifiedName).should.deep.equal(['Customers.CustomerRegistered']);
    });

    it('should read the top level file import', () => {
        result.value.fileImports.map(each => each.pattern).should.deep.equal(['**/*.play']);
    });

    it('should read the module file import', () => {
        result.value.modules[0].fileImports.map(each => each.pattern).should.deep.equal(['Orders/*.play']);
    });

    it('should read the feature file import', () => {
        result.value.modules[0].features[0].fileImports.map(each => each.pattern).should.deep.equal(['Slices/**/*.play']);
    });

    it('should locate the import where it is written', () => {
        result.value.modules[0].fileImports[0].location.should.deep.equal({ line: 6, column: 3, path: 'Document.play' });
    });

    it('should not mark what is written as a placement', () => {
        [result.value.modules[0].isPlacement, result.value.modules[0].features[0].isPlacement].should.deep.equal([false, false]);
    });

    it('should discover every import with the scope it is written in', () => {
        discoverImports([...source, 'feature Loose', '  import "Loose.play"', 'policy Staff', '  require authenticated'].join('\n')).map(found => `${found.scope.join('.')}:${found.fileImport.pattern}`).should.deep.equal([
            ':**/*.play',
            'Ordering:Orders/*.play',
            'Ordering.Orders:Slices/**/*.play',
            'Ordering.Orders.Drafts:Drafts.play',
            'Loose:Loose.play',
        ]);
    });
});
