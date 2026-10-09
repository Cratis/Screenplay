// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

const contracts = 'import Outside.Arrived from "other/store"\nmodule M\n  feature F\n    slice StateChange Facts\n      event Changed\n      public event Published\n';

for (const [name, expected] of [
    ['Changed', []],
    ['Missing', [DiagnosticCodes.UnknownEvent]],
    ['Published', [DiagnosticCodes.PublicEventRequiresOutboundTranslation]],
    ['Arrived', [DiagnosticCodes.ForeignPublicEventProduced]],
] as const) {
    describe(`when classifying seed events with ${name}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;

        beforeEach(() => {
            const application = parse(contracts + `seed\n  for "order-1"\n    ${name}\n`).value;
            diagnostics = publicEventDiagnostics(application);
        });

        it('should distinguish private fixtures from unresolved or forbidden public appends', () => {
            diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(expected);
        });
        if (expected.length > 0) {
            it('should identify the seed occurrence rather than the contract declaration', () => {
                diagnostics[0].location.line.should.equal(9);
                diagnostics[0].severity.should.equal(name === 'Missing' ? 'warning' : 'error');
            });
        }
    });
}
