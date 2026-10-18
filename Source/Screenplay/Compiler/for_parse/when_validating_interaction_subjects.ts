// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

const declarations = ['module M', '  feature F', '    slice StateView S', '      readmodel Item', '        status String', '        id Uuid', '      query Details => Item', '      command Retry', '        id Uuid', '      screen Details'];
const subjectMessage = "A guarded interaction requires exactly one nearest 'data' subject; component context and selection do not supply 'item'";

for (const trigger of ['click', 'double click', 'select']) {
    describe(`when validating ${trigger} interaction subjects`, () => {
        it('should warn at the binding header without a data subject', () => {
            const result = parse([...declarations, `        on ${trigger}`, '          when item.missing == null', '            notify info "Missing"'].join('\n'), 'screen.play');
            result.success.should.equal(true);
            result.diagnostics.should.deep.equal([{ code: 'PLAY0346', message: subjectMessage, severity: 'warning', location: { path: 'screen.play', line: 11, column: 9 }, arguments: {} }]);
        });
        it('should validate condition paths before action inputs and point to each offending line', () => {
            const result = parse([...declarations, '        data Item via query Details', `        on ${trigger}`, '          when item.missing == null', '            execute Retry', '              with unknown from item.status.missing'].join('\n'), 'screen.play');
            result.success.should.equal(true);
            result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location, diagnostic.message]).should.deep.equal([
                ['PLAY0345', 'warning', { path: 'screen.play', line: 13, column: 11 }, "Unknown or collection-valued subject field 'item.missing' - guarded actions require an item field path"],
                ['PLAY0348', 'warning', { path: 'screen.play', line: 15, column: 15 }, "Command 'Retry' has no argument property 'unknown'"],
                ['PLAY0345', 'warning', { path: 'screen.play', line: 15, column: 15 }, "Subject field 'item.status.missing' continues past scalar 'status'"],
            ]);
        });
        it('should resolve subjects declared after a binding inside template slots and sections', () => {
            parse([...declarations, '        template Page', '          content', '            section nested', `              on ${trigger}`, '                when item.status == null', '                  execute Retry', '                    with id from item.id', '            data Item via query Details'].join('\n')).diagnostics.should.deep.equal([]);
        });
    });
}

describe('when validating named interaction attachments', () => {
    it('should carry named behavior and use-site facts across workspace imports', () => {
        const result = compileApplication(new Map([
            ['application.play', 'import "behavior.play"\nmodule M\n  feature F\n    import "screen.play"'],
            ['behavior.play', 'behavior Shared\n  on click\n    when item.missing == null\n      execute Retry\n        with missing from item.id'],
            ['screen.play', 'slice StateView S\n  readmodel Item\n    id Uuid\n  query Details => Item\n  command Retry\n    id Uuid\n  screen Details\n    data Item via query Details\n    uses Shared\n    component App.Panel panel\n      uses Shared\n  screen Missing\n    uses Shared'],
        ]), ['application.play']);
        result.success.should.equal(true);
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.location.path, diagnostic.location.line, diagnostic.location.column]).should.deep.equal([
            ['PLAY0345', 'behavior.play', 3, 5], ['PLAY0348', 'behavior.play', 5, 9], ['PLAY0346', 'screen.play', 13, 5],
        ]);
    });
    it('should resolve command arguments independently in every attachment scope', () => {
        const source = ['behavior Shared', '  on click', '    when item.id == null', '      execute Retry', '        with id from item.id', 'module M', '  feature F', '    slice StateView First', '      readmodel Item', '        id Uuid', '      query Details => Item', '      command Retry', '        id Uuid', '      screen Details', '        data Item via query Details', '        uses Shared', '    slice StateView Second', '      readmodel Item', '        id Uuid', '      query Details => Item', '      command Retry', '        other Uuid', '      screen Details', '        data Item via query Details', '        uses Shared'];
        parse(source.join('\n')).diagnostics.map(diagnostic => [diagnostic.code, diagnostic.message]).should.deep.equal([['PLAY0348', "Command 'Retry' has no argument property 'id'"]]);
    });
    it('should not choose between duplicate named declarations including an empty behavior', () => {
        const source = ['behavior Shared', '  on click', '    when item.missing == null', '      notify info "Missing"', 'behavior Shared', ...declarations, '        data Item via query Details', '        uses Shared'];
        parse(source.join('\n')).diagnostics.filter(diagnostic => /^PLAY034[568]$/.test(diagnostic.code)).should.deep.equal([]);
    });
    it('should leave unresolved imported shapes and ambiguous command arguments unknown', () => {
        const source = ['import External.Unknown', 'behavior Shared', '  on click', '    when item.missing == null', '      execute Retry', '        with missing from item.anything', 'module M', '  feature F', '    slice StateChange First', '      command Retry', '        first Uuid', '    slice StateChange Second', '      command Retry', '        second Uuid', '    slice StateView S', '      screen Details', '        data Unknown via query Details', '        uses Shared'];
        parse(source.join('\n')).diagnostics.filter(diagnostic => /^PLAY034[568]$/.test(diagnostic.code)).should.deep.equal([]);
    });
});
