// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const declarations = 'eventsource Account\n  identifier String\n  stream All\n  stream Notes\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        period String\n';

function command(production: string) {
    return parse(declarations + production).value!.modules[0].features[0].slices[0].commands[0];
}

describe('when parsing production routes', () => {
    it('should retain a plain production override', () => {
        command('        produces E\n          stream Account.Notes\n            streamId = period').produces[0].stream!.stream.should.equal('Notes');
    });
    it('should retain a conditional production override under the event line', () => {
        command('        produces when period == "month"\n          E\n            stream Account.All').produces[0].stream!.stream.should.equal('All');
    });
    it('should retain an inline production override', () => {
        command('        produces event E\n          stream Account.All\n          period String = period').produces[0].stream!.stream.should.equal('All');
    });
    it('should keep an inline payload named stream as payload', () => {
        const produced = command('        produces event E\n          stream String = period').produces[0];
        (produced.stream === null).should.equal(true);
        produced.mappings[0].property.should.equal('stream');
    });
    it('should refuse malformed route headers without consuming the next production', () => {
        const result = parse(declarations + '        produces E\n          stream Account\n        produces F');
        result.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0504').should.equal(true);
        result.value!.modules[0].features[0].slices[0].commands[0].produces.length.should.equal(2);
    });
    it('should refuse duplicate routes and retain the first one', () => {
        const result = parse(declarations + '        produces E\n          for id\n          stream Account.All\n          stream Account.Notes\n            streamId = period');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0504']);
        result.value!.modules[0].features[0].slices[0].commands[0].produces[0].stream!.stream.should.equal('All');
    });
});

describe('when parsing observer filters', () => {
    const prefix = declarations + '      event E\n    slice Automation Follow\n';
    it('should refuse malformed reaction filters', () => {
        parse(prefix + '      reaction R\n        from Account.All.Extra\n        when E').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0638').should.equal(true);
    });
    it('should refuse duplicate reducer filters', () => {
        parse(prefix + '      readmodel V\n        id String\n      reducer R => V\n        from Account\n        from Account\n        on E').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0638']);
    });
    it('should refuse reducer filter children', () => {
        parse(prefix + '      readmodel V\n        id String\n      reducer R => V\n        from Account.All\n          streamId = "period"\n        on E').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0638').should.equal(true);
    });
    it('should refuse a stream belonging to another source', () => {
        parse(prefix + '      reaction R\n        from Account.Missing\n        when E').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0638').should.equal(true);
    });
});
