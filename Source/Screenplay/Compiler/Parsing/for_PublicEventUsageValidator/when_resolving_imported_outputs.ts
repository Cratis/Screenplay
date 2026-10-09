// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

for (const [name, expected] of [
    ['Arrived', [DiagnosticCodes.AmbiguousReference]],
    ['Outside.Arrived', [DiagnosticCodes.ForeignPublicEventProduced, DiagnosticCodes.InboundTranslationOutput]],
] as const) {
    describe(`when resolving imported outputs named ${name}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;

        beforeEach(() => {
            const application = parse(`import Outside.Arrived from "one/store"
import Another.Arrived from "two/store"
import Outside.Received from "one/store"
module M
  feature F
    slice Translate Receive
      direction inbound
      event Changed
      reaction Receive
        when Received
          produces ${name}
`).value;
            diagnostics = publicEventDiagnostics(application);
        });

        it('should leave ambiguous outputs unclassified and enforce boundaries on qualified foreign outputs', () => {
            diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(expected);
        });
        it('should locate every diagnostic on the output rather than its valid foreign input', () => {
            diagnostics.map(diagnostic => diagnostic.location.line).should.deep.equal(expected.map(() => 11));
        });
    });
}

describe('when resolving an operation output in an explicitly inbound translation', () => {
    let diagnostics: ReturnType<typeof publicEventDiagnostics>;

    beforeEach(() => {
        const application = parse('system Outside\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      operation Notify\n        uses Outside\n      reaction Receive\n        every 5 minutes\n          produces Notify\n').value;
        diagnostics = publicEventDiagnostics(application);
    });

    it('should not classify an operation as an unknown or private event', () => {
        diagnostics.should.deep.equal([]);
    });
});
