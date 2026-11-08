// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateView S\n      readmodel Row\n';
const graph = 'concept PersonId : Uuid\nmodule M\n  feature F\n    slice StateView S\n      graph G\n';
const codes = (source: string) => parse(source).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);
const model = (source: string) => parse(source).value.modules[0].features[0].slices[0].readModels[0];

describe('when parsing sinks', () => {
    it('should read a sink declaration and what it stores', () => {
        const result = parse('sink Reporting for readmodels\nsink People for graphs\n');
        result.value.sinks!.map(sink => `${sink.name}:${sink.stores}`).should.deep.equal(['Reporting:ReadModels', 'People:Graphs']);
    });

    it('should omit sinks from an application that declares none', () => {
        Object.hasOwn(parse('concept Id : Uuid').value, 'sinks').should.equal(false);
        Object.hasOwn(toSyntaxJson(parse('concept Id : Uuid').value) as object, 'sinks').should.equal(false);
    });

    it('should name the sink of a read model and its directive line', () => {
        const parsed = model('sink Reporting for readmodels\n' + prefix + '        id String\n        sink Reporting');
        parsed.sink!.should.equal('Reporting');
        parsed.directiveLocations!.sink.line.should.equal(7);
        (toSyntaxJson(parsed) as { sink: string }).sink.should.equal('Reporting');
    });

    it('should omit the sink of a read model that names none', () => {
        Object.hasOwn(model(prefix + '        id String'), 'sink').should.equal(false);
        Object.hasOwn(toSyntaxJson(model(prefix + '        id String')) as object, 'sink').should.equal(false);
    });

    it('should name the sink of a graph', () => {
        parse('sink People for graphs\n' + graph + '        sink People\n        node A\n          id PersonId key').value.modules[0].features[0].slices[0].graphs![0].sink!.should.equal('People');
    });

    it.each([['sink String', 'String'], ['sink Money[]', 'Money'], ['sink Money optional', 'Money'], ['@sink Money', 'Money']])('should keep a property named sink for %s', (line, type) => {
        const parsed = model(prefix + `        ${line}`);
        Object.hasOwn(parsed, 'sink').should.equal(false);
        parsed.properties[0].name.should.equal('sink');
        parsed.properties[0].type.name.should.equal(type);
    });

    it('should report each sink diagnostic', () => {
        codes('sink Reporting').should.deep.equal(['PLAY0916@1']);
        codes('sink A for readmodels\nsink a for readmodels\n' + prefix + '        sink A').should.deep.equal(['PLAY0917@2']);
        codes(prefix + '        sink Reporting').should.deep.equal(['PLAY0918@5']);
        codes('sink People for graphs\n' + prefix + '        sink People').should.deep.equal(['PLAY0919@6']);
        codes('sink Reporting for readmodels\n' + graph.replace('graph G\n', 'graph G\n        sink Reporting\n') + '        node A\n          id PersonId key').should.deep.equal(['PLAY0919@7']);
        codes('sink Reporting for readmodels\n' + prefix + '        passive\n        sink Reporting').should.deep.equal(['PLAY0920@7']);
        codes('sink Reporting for readmodels\n' + prefix + '        sink Reporting\n        sink Archive').should.deep.equal(['PLAY0921@7']);
        codes('sink Reporting for readmodels\n' + prefix + '        sink Reporting\n        sink Reporting').should.deep.equal(['PLAY0922@7']);
        codes('sink Reporting for readmodels\n' + prefix + '        id String').should.deep.equal(['PLAY0923@1']);
    });
});
