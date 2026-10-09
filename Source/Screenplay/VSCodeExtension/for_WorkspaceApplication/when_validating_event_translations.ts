// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';

const cases = [
    ["StateView", "projection Publisher => Published\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.EventTargetOutsideOutboundTranslation],
    ["StateView", "reducer Publisher => Changed\n        on Changed\n          file Reduce.cs", DiagnosticCodes.EventTargetOutsideOutboundTranslation],
    ["Translate", "direction outbound\n      projection Publisher => Changed\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.OutboundTranslationOutput],
    ["Translate", "direction outbound\n      projection Publisher => Published\n        from Arrived\n          id = $eventSourceId", DiagnosticCodes.OutboundTranslationInput],
    ["Translate", "capture Feed\n        source events\n          from Arrived\n        key id", DiagnosticCodes.EventsSourceOutsideInboundTranslation],
    ["Translate", "direction inbound\n      capture Feed\n        source events\n          from Changed\n        key id", DiagnosticCodes.InboundTranslationInput],
    ["Translate", "direction inbound\n      capture Feed\n        source events\n        key id", DiagnosticCodes.InvalidCaptureEventsSource],
    ["Translate", "direction inbound\n      capture Feed\n        source events\n          route /x\n        key id", DiagnosticCodes.InvalidCaptureEventsSource],
] as const;

describe('when surfacing event translation boundaries across workspace imports', () => {
    for (const [kind, body, code] of cases) {
        it(`should preserve ${code} in an imported ${kind} slice with ${body}`, () => {
            const application = new WorkspaceApplication();
            application.set('application.play', 'import Outside.Arrived from "other/store"\nmodule Sales\n  feature Orders\n    import "facts.play"\n    import "transfer.play"');
            application.set('facts.play', 'slice StateChange Facts\n  event Changed\n  public event Published\n  public event Second');
            const source = [`slice ${kind} Transfer`, ...(`      ${body}`).split('\n').map(line => line.slice(4))];
            application.set('transfer.play', source.join('\n'));
            const diagnostics = application.diagnosticsFor('transfer.play');
            const diagnostic = diagnostics.find(diagnostic => diagnostic.code === code)!;
            diagnostics.some(diagnostic => diagnostic.code === code && diagnostic.severity === 'error' && diagnostic.location.path === 'transfer.play').should.be.true;
            const issues = validateLines(source, { application: application.symbolsExcept('transfer.play'), placement: application.placementOf('transfer.play'), path: 'transfer.play', compilerDiagnostics: diagnostics });
            issues.some(issue => issue.code === code && issue.message === diagnostic.message && issue.severity === diagnostic.severity && issue.line === diagnostic.location.line - 1).should.be.true;
        });
    }
});
