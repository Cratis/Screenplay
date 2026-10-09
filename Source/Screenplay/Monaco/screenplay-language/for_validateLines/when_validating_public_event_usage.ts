// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes, parse } from '@cratis/screenplay-compiler';
import { validateLines } from '../validation';
import { scanDocument } from '../symbols';

const contracts = 'import Outside.Arrived from "other/store"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Second\n';
const cases = [
    ["StateChange", "command Send\n        produces Published", DiagnosticCodes.CommandProducesPublicEvent],
    ["StateChange", "command Send\n        produces Facts.Published", DiagnosticCodes.CommandProducesPublicEvent],
    ["StateView", "projection View\n        all", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateView", "projection View\n        remove with Arrived", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateView", "projection View\n        join related on id\n          with Arrived", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateView", "reducer View => View\n        on Arrived\n          file Receive.cs", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateChange", "constraint Unique\n        unique event Arrived", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateChange", "constraint Unique\n        unique event Changed\n        released by Arrived", DiagnosticCodes.ForeignPublicEventConsumer],
    ["Translate", "direction inbound\n      capture Data\n        append Published", DiagnosticCodes.InboundTranslationOutput],
    ["Automation", "reaction Send\n        when Changed\n          produces Published", DiagnosticCodes.PublicEventRequiresOutboundTranslation],
    ["Automation", "reaction Receive\n        when Arrived\n          produces Changed", DiagnosticCodes.ForeignPublicEventConsumer],
    ["StateView", "projection View\n        from Arrived", DiagnosticCodes.ForeignPublicEventConsumer],
    ["Translate", "direction outbound\n      reaction Send\n        when Published\n          produces Published", DiagnosticCodes.OutboundTranslationInput],
    ["Translate", "direction inbound\n      reaction Receive\n        when Arrived\n          produces Published", DiagnosticCodes.InboundTranslationOutput],
    ["Translate", "direction inbound\n      reaction Receive\n        when Arrived\n          produces Arrived", DiagnosticCodes.ForeignPublicEventProduced],
    ["Translate", "reaction Receive\n        when Arrived\n          produces Changed", DiagnosticCodes.PublicTranslationRequiresDirection],
    ["Translate", "direction outbound", DiagnosticCodes.OutboundPublicEventCount],
    ["Translate", "direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Second", DiagnosticCodes.OutboundPublicEventCount],
    ["Translate", "direction outbound\n      capture Data\n        append Published", DiagnosticCodes.TranslationConstructDirection],
    ["Translate", "direction inbound\n      reaction Receive\n        when Changed\n          produces Changed", DiagnosticCodes.InboundTranslationInput],
    ["Translate", "direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Changed", DiagnosticCodes.OutboundTranslationOutput],
 ] as const;

describe('when validating public event boundaries in the editor', () => {
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
