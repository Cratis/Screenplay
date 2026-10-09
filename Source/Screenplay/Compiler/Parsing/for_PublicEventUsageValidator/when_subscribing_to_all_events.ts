// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

const contracts = 'import Outside.Arrived from "other/store"\nimport Legacy.Unknown\nmodule M\n  feature F\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Imported from "remote/store"\n';

for (const [kind, direction, expected] of [
    ['StateView', '', [DiagnosticCodes.ForeignPublicEventConsumer, DiagnosticCodes.ForeignPublicEventConsumer]],
    ['Translate', 'inbound', [DiagnosticCodes.InboundTranslationInput, DiagnosticCodes.InboundTranslationInput]],
    ['Translate', 'outbound', [DiagnosticCodes.OutboundTranslationInput, DiagnosticCodes.ForeignPublicEventConsumer, DiagnosticCodes.OutboundTranslationInput, DiagnosticCodes.ForeignPublicEventConsumer, DiagnosticCodes.OutboundTranslationInput, DiagnosticCodes.OutboundPublicEventCount]],
    ['Translate', '', [DiagnosticCodes.ForeignPublicEventConsumer, DiagnosticCodes.ForeignPublicEventConsumer, DiagnosticCodes.PublicTranslationRequiresDirection]],
] as const) {
    describe(`when subscribing to all events in ${direction || 'undirected'} ${kind}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;

        beforeEach(() => {
            const application = parse(contracts + `    slice ${kind} View\n${direction ? `      direction ${direction}\n` : ''}      projection View\n        all\n`).value;
            diagnostics = publicEventDiagnostics(application);
        });

        it('should classify every local and public imported contract without subscribing to opaque imports', () => {
            diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(expected);
        });
        it('should report boundary violations as errors', () => {
            diagnostics.every(diagnostic => diagnostic.severity === 'error').should.be.true;
        });
    });
}
