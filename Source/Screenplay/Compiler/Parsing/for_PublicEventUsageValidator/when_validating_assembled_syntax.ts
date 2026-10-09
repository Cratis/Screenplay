// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';
import { EventVisibility } from '../../Syntax/EventVisibility';
import { TranslationDirection } from '../../Syntax/TranslationDirection';
import { ApplicationSyntax, SliceSyntax } from '../../Syntax/Structure';

const contracts = 'import Outside.Arrived from "other/store"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n';
const replacingLastSlice = (application: ApplicationSyntax, slice: SliceSyntax): ApplicationSyntax => {
    const module = application.modules[0];
    const feature = module.features[0];
    return { ...application, modules: [{ ...module, features: [{ ...feature, slices: [...feature.slices.slice(0, -1), slice] }] }] };
};

describe('when validating assembled public contracts', () => {
    it('should not let an operation shadow a subscription to a foreign event', () => {
        const application = parse(contracts + '    slice Automation Receive\n      reaction Receive\n        when Arrived\n          produces Changed\n').value;
        const slice = application.modules[0].features[0].slices[1];
        const operation = parse('module M\n  feature F\n    slice StateChange S\n      operation Arrived on Outside\n').value.modules[0].features[0].slices[0].operations![0];
        publicEventDiagnostics(replacingLastSlice(application, { ...slice, operations: [operation] })).map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.ForeignPublicEventConsumer);
    });
    it('should enforce command production boundaries on programmatic syntax', () => {
        const application = parse(contracts + '    slice StateChange Send\n      command Send\n        produces Changed\n').value;
        const slice = application.modules[0].features[0].slices[1];
        const command = slice.commands[0];
        publicEventDiagnostics(replacingLastSlice(application, { ...slice, commands: [{ ...command, produces: [{ ...command.produces[0], event: 'Published' }] }] })).map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.CommandProducesPublicEvent);
    });
    it('should reuse the private-origin invariant on programmatic syntax', () => {
        const application = parse(contracts).value;
        const slice = application.modules[0].features[0].slices[0];
        publicEventDiagnostics(replacingLastSlice(application, { ...slice, events: [{ ...slice.events[0], visibility: EventVisibility.Private, origin: 'foreign' }, ...slice.events.slice(1)] })).map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.InvalidEventDeclaration);
    });
    it('should require a direction for public declarations even without operational edges', () => {
        parse('module M\n  feature F\n    slice Translate S\n      public event Published\n').diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.PublicTranslationRequiresDirection);
    });
    it('should distinguish every mapping from all subscriptions', () => {
        parse(contracts + '    slice StateView View\n      projection View\n        from Changed\n        every\n').diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.ForeignPublicEventConsumer);
        parse(contracts + '    slice StateView View\n      projection View\n        all\n').diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.ForeignPublicEventConsumer);
    });
    for (const body of ['clear with Arrived', 'remove via join on Arrived', 'children rows identified by id\n          from Arrived', 'nested row\n          from Arrived']) {
        it(`should collect nested projection input ${body}`, () => {
            parse(contracts + `    slice StateView View\n      projection View\n        ${body}\n`).diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.ForeignPublicEventConsumer);
        });
    }
    for (const body of ['children rows identified by id\n          append Published\n            when added', 'nested row\n          append Published\n            when changed']) {
        it(`should collect nested capture output ${body}`, () => {
            parse(contracts + `    slice Translate Receive\n      direction inbound\n      capture Receive\n        ${body}\n`).diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.InboundTranslationOutput);
        });
    }
    it('should collect refusal branch outputs', () => {
        const result = parse(contracts + '    slice Automation Send\n      reaction Send\n        when Changed\n          invokes Claim\n            on refused\n              produces Published\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.PublicEventRequiresOutboundTranslation);
    });
    it('should ignore public events mentioned only in specifications', () => {
        const result = parse(contracts + '    slice Translate Legacy\n      specification Fixture\n        given Arrived\n        when append Published\n        then Published\n');
        result.diagnostics.filter(diagnostic => /^PLAY059[6-9]$|^PLAY060[0-6]$/.test(diagnostic.code)).should.deep.equal([]);
    });
    it('should resolve nested scopes and qualified local imports before public import metadata', () => {
        const result = compileApplication(new Map([
            ['root.play', 'import Sales.Orders.Facts.Changed\nmodule Sales\n  feature Orders\n    import "facts.play"\n    feature Nested\n      import "send.play"\n'],
            ['facts.play', 'slice StateChange Facts\n  event Changed\n  public event Published\n'],
            ['send.play', 'slice Translate Send\n  direction outbound\n  reaction Send\n    when Changed\n      produces Published\n'],
        ]), ['root.play']);
        result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
    });
    it('should not classify opaque legacy imports as private', () => {
        const result = parse('import Outside.Unknown\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      event Changed\n      reaction Receive\n        when Unknown\n          produces Changed\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.InboundTranslationInput);
    });
    it('should not classify ambiguous imports as private', () => {
        const result = parse('import Outside.Unknown from "one"\nimport Another.Unknown from "two"\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      event Changed\n      reaction Receive\n        when Unknown\n          produces Changed\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.AmbiguousReference);
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.InboundTranslationInput);
    });
    it('should count logical event types rather than generations or append occurrences', () => {
        const result = parse('module M\n  feature F\n    slice Translate Publish\n      direction outbound\n      event Changed\n      public event Published generation 1\n      public event Published generation 2\n      reaction Publish\n        when Changed\n          produces Published\n          produces Published\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.OutboundPublicEventCount);
        result.value.modules[0].features[0].slices[0].direction!.should.equal(TranslationDirection.Outbound);
    });
});
