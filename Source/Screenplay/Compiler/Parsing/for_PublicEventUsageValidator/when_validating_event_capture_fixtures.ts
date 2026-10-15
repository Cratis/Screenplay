// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

const source = (property: string, key: string, steps = '') => `module M
  feature F
    slice Translate Receive
      direction inbound
      event Arrived from "store"
        ${property}
      capture Feed
        source events
          from Arrived
        key ${key}
${steps}`;

for (const [property, key] of [['other String', 'id'], ['id String optional', 'id'], ['id String', '$eventSourceId']]) {
    describe(`when validating event capture key ${key} against ${property}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        beforeEach(() => { diagnostics = publicEventDiagnostics(parse(source(property, key)).value); });
        it('should reject the key', () => diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.CaptureEventsKeyNotRequired]));
        it('should locate the capture', () => diagnostics[0].location.line.should.equal(7));
    });
}

for (const step of ['given', 'when']) {
    describe(`when validating ${step} capture for an events source`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        beforeEach(() => { diagnostics = publicEventDiagnostics(parse(source('id String', 'id', `      specification S\n        ${step} capture Feed\n          id = "one"\n`)).value); });
        it('should require an event fixture', () => diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.CaptureEventsRecordFixture]));
        it('should locate the record step', () => diagnostics[0].location.line.should.equal(12));
    });
}

describe('when validating an event capture with typed event fixtures', () => {
    let diagnostics: ReturnType<typeof publicEventDiagnostics>;
    beforeEach(() => { diagnostics = publicEventDiagnostics(parse(source('id String', 'id', '      specification S\n        given Arrived\n          id = "one"\n        when append Arrived\n          id = "two"\n')).value); });
    it('should accept the fixtures', () => diagnostics.should.deep.equal([]));
});
