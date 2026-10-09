// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes, parse } from '@cratis/screenplay-compiler';
import { validateLines } from '../validation';
import { scanDocument } from '../symbols';

const contracts = 'import Outside.Arrived from "other/store"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Second\n';
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

describe('when validating event translations in the editor', () => {
    for (const [kind, body, code] of cases) {
        it(`should preserve native ${code} severity message and location for ${body}`, () => {
            const source = contracts + `    slice ${kind} Transfer\n      ${body}\n`;
            const diagnostic = parse(source).diagnostics.find(diagnostic => diagnostic.code === code)!;
            const issues = validateLines(source.split('\n'));
            issues.some(issue => issue.code === code && issue.message === diagnostic.message && issue.severity === diagnostic.severity && issue.line === diagnostic.location.line - 1 && issue.startColumn === diagnostic.location.column).should.be.true;
        });
    }
    it('should use the assembled inventory for split editor buffers', () => {
        const source = 'module Sales\n  feature Orders\n    slice Automation Receive\n      reaction Receive\n        when Arrived\n          produces Facts.Changed\n';
        validateLines(source.split('\n'), { application: scanDocument(contracts.split('\n')) }).some(issue => issue.code === DiagnosticCodes.ForeignPublicEventConsumer && issue.line === 4).should.be.true;
    });
});
