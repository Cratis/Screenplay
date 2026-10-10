// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { QueryParameterSyntax } from '../Syntax/Queries';
import { PropertyMappingSyntax } from '../Syntax/Expressions';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';

describe('when parsing read-model keys', () => {
    it('should preserve explicit marks and authored query part order', () => {
        const result = parse(prefix + '      readmodel Row\n        id String key\n        period Int key\n      query Find => Row optional\n        by\n          period Int\n          id String from $context.tenant');
        result.diagnostics.should.be.empty;
        const slice = result.value!.modules[0].features[0].slices[0];
        slice.readModels[0].properties.every(property => property.isKey).should.be.true;
        slice.queries[0].byParts!.map(part => part.name).should.deep.equal(['period', 'id']);
        const visited: string[] = [];
        class Walker extends ScreenplaySyntaxWalker {
            visitQueryParameter(syntax: QueryParameterSyntax): void { visited.push(syntax.name); super.visitQueryParameter(syntax); }
        }
        new Walker().visitQuery(slice.queries[0]);
        visited.should.deep.equal(['period', 'id']);
    });

    it.each(['String optional key', 'String[] key', 'String generated key', 'String key optional', 'String key key'])('should refuse invalid key modifiers %s', type => {
        parse(prefix + `      readmodel Row\n        id ${type}`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKey).should.be.true;
    });
    it.each(['command C', 'event E'])('should refuse key outside a view: %s', owner => {
        parse(prefix + `      ${owner}\n        id String key`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKey).should.be.true;
    });
    it('should refuse composite-typed parts in multipart keys', () => {
        parse('type Key\n  value String\n' + prefix + '      readmodel Row\n        id Key key\n        period Int key').diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKey).should.be.true;
    });
    it('should refuse a nested key', () => {
        parse(prefix + '      readmodel Row\n        value String\n          nested String key').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.InvalidReadModelKey]);
    });
    it('should diagnose duplicate key properties', () => {
        parse(prefix + '      readmodel Row\n        id String key\n        id String key').diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKey).should.be.true;
    });
    it('should keep a property named key', () => {
        parse(prefix + '      readmodel Row\n        key String').diagnostics.should.be.empty;
    });
    it.each([
        '        by\n          id String',
        '        by\n          id String\n          id String',
        '        by id String\n        by\n          id String\n          period Int',
        '        by\n          id String\n          period Int\n        by id String',
    ])('should reject invalid by shapes %s', body => {
        parse(prefix + '      query Find => Row optional\n' + body).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKeyLookup).should.be.true;
    });
    it('should capture reads mappings and walk them', () => {
        const result = parse(prefix + '      command C\n        reads Row as row\n          by\n            period = month\n            id = id');
        result.diagnostics.should.be.empty;
        const read = dependencySourcesOf(result.value!.modules[0].features[0].slices[0].commands[0]).reads![0];
        read.byParts!.map(part => part.property).should.deep.equal(['period', 'id']);
        const visited: string[] = [];
        class Walker extends ScreenplaySyntaxWalker {
            visitPropertyMapping(syntax: PropertyMappingSyntax): void { visited.push(syntax.property); super.visitPropertyMapping(syntax); }
        }
        new Walker().visitReads(read);
        visited.should.deep.equal(['period', 'id']);
    });
    it('should capture trigger reads', () => {
        const result = parse(prefix + '      reaction R\n        when E\n          reads Row\n            by\n              period = month\n              id = id');
        result.diagnostics.should.be.empty;
        dependencySourcesOf(result.value!.modules[0].features[0].slices[0].reactions[0].triggers[0]).reads![0].byParts!.length.should.equal(2);
    });
    it.each([
        '          by\n            id = id',
        '          by\n            id = id\n            id = other',
        '          by\n            invalid\n            id = id',
    ])('should reject invalid reads blocks %s', body => {
        parse(prefix + '      command C\n        reads Row\n' + body).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidReadModelKeyLookup).should.be.true;
    });
    it('should reject other reads children', () => {
        parse(prefix + '      command C\n        reads Row\n          other').diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.ReadsWithChildren).should.be.true;
    });
});
