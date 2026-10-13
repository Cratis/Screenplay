// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

const source = (origins: string[]): string => origins.map((origin, index) => `import Outside.E${index} from "${origin}"`).join('\n') + '\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      capture Feed\n        source events\n' + origins.map((_, index) => `          from E${index}`).join('\n') + '\n        key id\n';
const message = "The 'from' events of capture 'Feed' originate from several event stores (a/store, z/store); a capture reads one event store - split it into one capture per origin.";

describe('when validating capture events from one origin', () => {
    let diagnostics: ReturnType<typeof publicEventDiagnostics>;
    beforeEach(() => { diagnostics = publicEventDiagnostics(parse(source(['store', 'store'])).value); });
    it('should accept the capture', () => diagnostics.should.deep.equal([]));
});

for (const origins of [['z/store', 'a/store'], ['z/store', 'a/store', 'z/store']]) {
    describe(`when validating ${origins.length} capture events from two origins`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        beforeEach(() => { diagnostics = publicEventDiagnostics(parse(source(origins)).value); });
        it('should report one diagnostic', () => diagnostics.should.have.lengthOf(1));
        it('should report the capture origins code', () => diagnostics[0].code.should.equal(DiagnosticCodes.CaptureEventsFromSeveralOrigins));
        it('should report an error', () => diagnostics[0].severity.should.equal('error'));
        it('should name the capture and sorted distinct origins', () => diagnostics[0].message.should.equal(message));
        it('should locate the source events block', () => diagnostics[0].location.line.should.equal(origins.length + 6));
    });
}
