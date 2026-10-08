// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { productionDestinationDiagnostics } from '../Diagnostics/ProductionDestinationDiagnostics';
import { parseFolder } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

const header = ['module M', '  feature F', '    slice StateChange S'];

describe('when reporting constraint property diagnostics', () => {
    it('should check additional rules against inline event declarations', () => {
        const result = parse([...header, '      command C', '        produces event E', '          value String', '      constraint UniqueValue',
            '        unique value on E', '        and unique missing on E'].join('\n'));
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0391').map(diagnostic => diagnostic.location.line).should.deep.equal([8]);
    });
    it('should report each missing direct field but leave unknown events and paths undecided', () => {
        const result = parse([...header, '      event E', '      constraint UniqueValue', '        unique first, second, nested.missing on E',
            '      constraint External', '        unique missing on Unknown'].join('\n'));
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0391').map(diagnostic => diagnostic.location.line).should.deep.equal([6, 6]);
    });
    it('should use declarations in other files without reporting twice', () => {
        const result = parseFolder([
            { path: 'event.play', source: ['module M', '  feature F', '    slice StateView Declaration', '      event E', '        value String'].join('\n') },
            { path: 'constraint.play', source: [...header, '      constraint UniqueValue', '        unique missing on E'].join('\n') },
        ]);
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0391').map(diagnostic => diagnostic.location.path).should.deep.equal(['constraint.play']);
    });
});

describe('when reporting omitted production destinations', () => {
    it('should preserve information severity and explicit routing advice', () => {
        const result = parse([...header, '      event E', '      command C', '        id String identifier', '        produces E'].join('\n'));
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.line]).should.deep.equal([['PLAY0478', 'information', 7]]);
        result.success.should.be.true;
    });
    it.each([
        ['no identifier', '        id String', ['        produces E']],
        ['optional identifier', '        id String optional identifier', ['        produces E']],
        ['collection identifier', '        id String[] identifier', ['        produces E']],
        ['explicit destination', '        id String identifier', ['        produces E', '          for id']],
        ['inline declaration', '        id String identifier', ['        produces event Inline']],
        ['conditional production', '        id String identifier', ['        produces when id == "a"', '          E']],
        ['operation', '        id String identifier', ['        produces Notify']],
    ])('should not advise for %s', (_name, property, production) => {
        const result = parse([...header, '      event E', '      operation Notify', '      command C', property, ...production].join('\n'));
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0478').should.deep.equal([]);
    });
    it('should not advise when the syntax has multiple identifiers', () => {
        const application = parse([...header, '      event E', '      command C', '        id String identifier', '        other String', '        produces E'].join('\n')).value;
        const module = application.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        const command = slice.commands[0];
        const changed = { ...command, properties: command.properties.map(property => ({ ...property, isIdentifier: true })) };
        const syntax = { ...application, modules: [{ ...module, features: [{ ...feature, slices: [{ ...slice, commands: [changed] }] }] }] };
        productionDestinationDiagnostics(syntax).should.deep.equal([]);
    });
    it('should advise once using a declaration from a merged file', () => {
        const result = parseFolder([
            { path: 'event.play', source: ['module M', '  feature F', '    slice StateView Declaration', '      event E'].join('\n') },
            { path: 'command.play', source: [...header, '      command C', '        id String identifier', '        produces E'].join('\n') },
        ]);
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0478').map(diagnostic => diagnostic.location.path).should.deep.equal(['command.play']);
    });
});

describe('when preserving legacy absence assertion behavior', () => {
    it.each(['then no readmodel V for', 'then no readmodel V for key', 'then no readmodel V for 1 exactly'])('should skip malformed %s without PLAY0453', assertion => {
        const result = parse([...header, '      specification Missing', `        ${assertion}`].join('\n'));
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0453').should.deep.equal([]);
    });
});
