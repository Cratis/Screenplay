// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, publicEventDiagnostics } from '../ScreenplayCompiler';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { dependencySources, dependencySourcesOf } from '../Syntax/DependencySources';

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

describe('when validating public event usage', () => {
    for (const [kind, body, code] of cases) {
        it(`should report ${code} for ${kind} ${body}`, () => {
            const result = parse(contracts + `    slice ${kind} Transfer\n      ${body}\n`);
            result.diagnostics.some(diagnostic => diagnostic.code === code && diagnostic.severity === 'error').should.be.true;
        });
    }
    for (const body of ["direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed", "direction outbound\n      reaction Send\n        when Changed\n          produces Published", "reaction Receive\n        when Changed\n          produces Changed", "direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Published", "direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed\n          produces Changed"]) {
        it(`should accept translation ${body}`, () => {
            const result = parse(contracts + `    slice Translate Transfer\n      ${body}\n`);
            result.diagnostics.should.deep.equal([]);
            result.success.should.be.true;
        });
    }

    it('should prefer the nearest local event over an import', () => {
        parse(contracts + '    slice Translate Send\n      direction outbound\n      event Arrived\n      reaction Send\n        when Arrived\n          produces Published\n').success.should.be.true;
    });
    it('should resolve across merged files with consumer locations', () => {
        const result = compileApplication(new Map([['contracts.play', contracts], ['consumer.play', 'module Sales\n  feature Orders\n    slice Automation Receive\n      reaction Receive\n        when Arrived\n          produces Facts.Changed\n']]));
        result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.ForeignPublicEventConsumer && diagnostic.location.path === 'consumer.play').should.be.true;
    });
    for (const [name, code] of [['Published', DiagnosticCodes.PublicEventRequiresOutboundTranslation], ['Arrived', DiagnosticCodes.ForeignPublicEventProduced]]) {
        it(`should refuse seeding ${name}`, () => {
            parse(contracts + `seed\n  for "order-1"\n    ${name}\n`).diagnostics.map(diagnostic => diagnostic.code).should.contain(code);
        });
    }
    it('should leave ambiguous outputs unclassified', () => {
        const result = parse(contracts + '    slice StateChange Other\n      event Published\n    slice Translate Send\n      direction outbound\n      reaction Send\n        when Changed\n          produces Published\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.AmbiguousReference);
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.OutboundTranslationOutput);
    });
    it('should leave unresolved outputs unclassified', () => {
        const result = parse(contracts + '    slice Translate Receive\n      direction inbound\n      reaction Receive\n        when Arrived\n          produces Missing\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.UnknownEvent);
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.InboundTranslationOutput);
    });
    it('should validate programmatic qualified inputs without operation shadowing', () => {
        const application = parse(contracts + '    slice Translate Receive\n      direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed\n').value;
        const slice = application.modules[0].features[0].slices[1];
        const trigger = slice.reactions[0].triggers[0];
        const altered = { ...slice, reactions: [{ ...slice.reactions[0], triggers: [{ ...trigger, source: { ...trigger.source, name: '.Sales..Orders.Facts.Changed.' } }] }] };
        dependencySources.set(altered, dependencySourcesOf(slice));
        const rewritten = { ...application, modules: [{ ...application.modules[0], features: [{ ...application.modules[0].features[0], slices: [application.modules[0].features[0].slices[0], altered] }] }] };
        publicEventDiagnostics(rewritten).map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.InboundTranslationInput);
    });
});
