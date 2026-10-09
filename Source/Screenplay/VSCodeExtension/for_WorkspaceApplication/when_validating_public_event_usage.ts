// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';

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

describe('when surfacing public event boundaries across workspace imports', () => {
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
