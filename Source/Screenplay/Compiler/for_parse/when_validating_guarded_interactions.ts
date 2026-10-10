// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { Diagnostic } from '../Diagnostics/Diagnostic';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

const declarations = ['module M', '  feature F', '    slice StateView S', '      readmodel Item', '        status String', '        ready Bool', '      query Details => Item', '      screen Details', '        data Item via query Details'];
const alternatives = ['          when item.status == "open"', '            notify info "Open"', '          when item.status == "open" and item.ready == true', '            notify info "Ready"'];
const message = "This 'when' alternative is shadowed by earlier alternatives in this interaction";

for (const trigger of ['click', 'double click', 'select']) {
    describe(`when validating shadowed ${trigger} alternatives`, () => {
        let diagnostics: readonly Diagnostic[];
        beforeEach(() => {
            diagnostics = parse([...declarations, `        on ${trigger}`, ...alternatives].join('\n'), 'screen.play').diagnostics;
        });
        it('should warn with the interaction message', () => {
            diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.message]).should.deep.equal([['PLAY0347', 'warning', message]]);
        });
        it('should point to the later branch header', () => {
            diagnostics.map(diagnostic => diagnostic.location).should.deep.equal([{ line: 13, column: 11, path: 'screen.play' }]);
        });
    });
}

describe('when validating non-shadowed interaction alternatives', () => {
    it('should preserve distinct alternatives', () => {
        parse([...declarations, '        on click', ...alternatives.slice(0, 2), '          when item.status == "closed"', '            notify info "Closed"'].join('\n')).diagnostics.should.deep.equal([]);
    });
    it('should bound disjunction expansion to 64 terms', () => {
        const disjunction = Array.from({ length: 65 }, (_, index) => `item.status == "${index}"`).join(' or ');
        parse([...declarations, '        on click', `          when ${disjunction}`, '            notify info "Many"', '          when item.status == "0"', '            notify info "Zero"'].join('\n')).diagnostics.should.deep.equal([]);
    });
    it('should not infer coverage from a nonliteral operand', () => {
        const source = [...declarations, '        on click', '          when item.status == item.other', '            notify info "First"', '          when item.status == item.other', '            notify info "Second"'];
        parse(source.join('\n')).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0344', 'PLAY0344']);
    });
});

describe('when validating interaction shadowing after parsing', () => {
    it('should report later parse errors before shadowing', () => {
        const source = [...declarations, '        on click', ...alternatives, '        unexpected'];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0103@15', 'PLAY0347@13']);
    });
    it('should validate named behaviors before inline screen bindings', () => {
        const source = [...declarations, '        on click', ...alternatives, 'behavior Shared', '  on click', '    when item.ready == true', '      notify info "First"', '    when item.ready == true', '      notify info "Second"'];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@19', 'PLAY0347@13']);
    });
    it('should walk structural child collections before enclosing inline bindings', () => {
        const branch = ['when item.ready == true', '  notify info "First"', 'when item.ready == true', '  notify info "Second"'];
        const indent = (depth: number) => branch.map(line => ' '.repeat(depth) + line);
        const source = ['module M', '  on click', ...indent(4), '  feature F', '    on click', ...indent(6), '  screen template Page', '    on click', ...indent(6), 'behavior Shared', '  on click', ...indent(4), '  on select', ...indent(4)];
        parse(source.join('\n')).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0347').map(diagnostic => diagnostic.location.line).should.deep.equal([23, 28, 17, 11, 5]);
    });
    it('should retain literal conditions even if their operands are unsupported', () => {
        const source = ['behavior Shared', '  on submit', '    when status == "open"', '      notify info "First"', '    when status == "open"', '      notify info "Second"'];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0344@3', 'PLAY0344@5', 'PLAY0560@2', 'PLAY0347@5']);
    });
    it('should preserve guarded action and interaction ordering inside a screen', () => {
        const source = [...declarations, '      command Retry', '      screen Actions', '        data Item via query Details', '        on click', ...alternatives, '        action "Again"', '          when item.ready == true execute Retry', '          when item.ready == true execute Retry', '        on select', ...alternatives];
        parse(source.join('\n')).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0347').map(diagnostic => diagnostic.location.line).should.deep.equal([16, 20, 24]);
    });
    it('should validate component bindings before bindings in their outlets', () => {
        const source = [...declarations, '        component App.Panel panel', '          outlet content', '            on click', ...alternatives.map(line => `    ${line}`), '          on select', ...alternatives.map(line => `  ${line}`)];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@20', 'PLAY0347@15']);
    });
    it('should follow table, section and template slot attachments', () => {
        const source = [...declarations, '        table Item', '          on click', ...alternatives.map(line => `  ${line}`), '        section nested', '          on select', ...alternatives.map(line => `  ${line}`), '        template Page', '          actions', '            on double click', ...alternatives.map(line => `    ${line}`)];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@14', 'PLAY0347@20', 'PLAY0347@27']);
    });
    it('should follow nested sections and template slots inside component outlets', () => {
        const source = [...declarations, '        component App.Panel panel', '          outlet content', '            section nested', '              template Page', '                actions', '                  on click', ...alternatives.map(line => `          ${line}`)];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@18']);
    });
    it('should not mistake screen templates for screen declarations', () => {
        const source = ['module M', '  screen template Page', '    on click', '      when item.ready == true', '        notify info "First"', '      when item.ready == true', '        notify info "Second"'];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@6']);
    });
    it('should ignore binding-shaped text inside fenced code', () => {
        const source = [...declarations, '        ```typescript', '        on click', ...alternatives, '        ```', '', '        // A real binding follows.', '        on select', ...alternatives];
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0347@22']);
    });
    it('should retain alternatives for merged workspace validation', () => {
        const result = compileApplication(new Map([['application.play', 'import "screen.play"'], ['screen.play', [...declarations, '        on click', ...alternatives].join('\n')]]), ['application.play']);
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.location.path, diagnostic.location.line]).should.deep.equal([['PLAY0347', 'screen.play', 13]]);
    });
});
