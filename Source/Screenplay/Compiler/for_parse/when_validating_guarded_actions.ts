// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import vectors from '../Conformance/diagnostics.json';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

const guardedCode = (code: string) => /^PLAY034[5-8]$/.test(code);
const cases = vectors.cases.filter(vector => vector.name.startsWith('Guarded') && 'validate' in vector && vector.validate === true);

const declarations = ['module M', '  feature F', '    slice StateView S', '      readmodel Item', '        status String', '        id Uuid', '      query Details => Item', '      command Retry', '        id Uuid', '      screen Details'];
const action = ['action "Again"', '  when item.status == "open" execute Retry', '    with id from item.id'];
const indent = (lines: readonly string[], depth: number) => lines.map(line => ' '.repeat(depth) + line);

describe('when validating guarded screen actions', () => {
    it.each(cases)('should preserve warning severity and source success for $name', vector => {
        const result = parse(vector.source.join('\n'), 'application.play');
        result.success.should.equal(true);
        result.diagnostics.every(diagnostic => guardedCode(diagnostic.code) && diagnostic.severity === 'warning' && diagnostic.location.path === 'application.play').should.equal(true);
    });

    it('should resolve a subject declared after the action', () => {
        parse([...declarations, ...indent(action, 8), '        data Item via query Details'].join('\n')).diagnostics.should.deep.equal([]);
    });

    it('should inherit the nearest subject through template slots and nested sections', () => {
        parse([...declarations, '        data Item via query Details', '        template Page', '          actions', '            section nested', ...indent(action, 14)].join('\n')).diagnostics.should.deep.equal([]);
    });

    it('should replace inherited data with the slot data subject', () => {
        const source = [...declarations, '        data Item via query Details', '        template Page', '          actions', '            data Item via query Details', '            data Item via query Details', ...indent(action, 12)].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0346']);
    });

    it('should skip unknown and ambiguous nested shapes without guessing a field', () => {
        const source = ['import External.Unknown', 'type Duplicate', '  value String', 'type Duplicate', '  other String', 'module M', '  feature F', '    slice StateView S', '      readmodel Item', '        external Unknown', '        ambiguous Duplicate', '      query Details => Item', '      command Retry', '      screen Details', '        data Item via query Details', '        action "Again"', '          when item.external.missing == null execute Retry', '          when item.ambiguous.missing == null execute Retry'].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });

    it('should keep competing command declarations unknown instead of guessing arguments', () => {
        const source = ['module M', '  feature F', '    slice StateChange First', '      command Retry', '        first Uuid', '    slice StateChange Second', '      command Retry', '        second Uuid', '    slice StateView S', '      readmodel Item', '        status String', '        id Uuid', '      query Details => Item', '      screen Details', '        data Item via query Details', ...indent(action, 8)].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });

    it('should leave duplicate subject fields unresolved', () => {
        const source = [...declarations.slice(0, 6), '        status String', ...declarations.slice(6), '        data Item via query Details', ...indent(action, 8)].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });

    it('should not prove shadowing from a nonliteral operand or its logical parent', () => {
        const source = [...declarations, '        data Item via query Details', '        action "Again"', '          when item.status == item.other execute Retry', '          when item.status == item.other and item.id == null execute Retry', '          when item.id == null execute Retry'].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });

    it('should bound disjunction expansion as well as conjunction expansion', () => {
        const disjunction = Array.from({ length: 65 }, (_, index) => `item.status == "${index}"`).join(' or ');
        const source = [...declarations, '        data Item via query Details', '        action "Again"', `          when ${disjunction} execute Retry`, '          when item.status == "0" execute Retry'].join('\n');
        parse(source).diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });

    it('should follow declared read-model shapes inside a subject', () => {
        const source = ['module M', '  feature F', '    slice StateView S', '      readmodel Child', '        status String', '      readmodel Item', '        child Child', '      query Details => Item', '      command Retry', '      screen Details', '        data Item via query Details', '        action "Again"', '          when item.child.status == "open" execute Retry'].join('\n');
        parse(source).diagnostics.should.deep.equal([]);
    });

    it('should resolve imported workspace shapes only after merging files', () => {
        const result = compileApplication(new Map([
            ['application.play', 'module M\n  feature F\n    import "declarations.play"\n    import "screen.play"'],
            ['declarations.play', 'slice StateChange Commands\n  command Retry\n    id Uuid\nslice StateView Views\n  readmodel Item\n    status String\n    id Uuid\n  query Details => Item'],
            ['screen.play', 'slice StateView S\n  screen Details\n    data Views.Item via query Views.Details\n    action "Again"\n      when item.missing == null execute Commands.Retry\n        with missing from item.id'],
        ]), ['application.play']);
        result.diagnostics.filter(diagnostic => guardedCode(diagnostic.code)).map(diagnostic => [diagnostic.code, diagnostic.location.path, diagnostic.location.line]).should.deep.equal([
            ['PLAY0345', 'screen.play', 5], ['PLAY0348', 'screen.play', 6],
        ]);
    });
});
