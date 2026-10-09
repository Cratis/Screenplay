// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { consumedEvents } from '../Syntax/CaptureEventsSource';

const contracts = 'import Outside.Arrived from "other/store"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Second\n';
const outbound = '    slice Translate Publish\n      direction outbound\n      projection Publisher => Published\n        from Changed\n          id = $eventSourceId\n';

const cases = [
    ['StateView', 'projection Publisher => Published\n        from Changed\n          id = $eventSourceId', DiagnosticCodes.EventTargetOutsideOutboundTranslation],
    ['StateView', 'reducer Publisher => Changed\n        on Changed\n          file Reduce.cs', DiagnosticCodes.EventTargetOutsideOutboundTranslation],
    ['Translate', 'direction inbound\n      projection Publisher => Published\n        from Changed\n          id = $eventSourceId', DiagnosticCodes.EventTargetOutsideOutboundTranslation],
    ['Translate', 'direction outbound\n      projection Publisher => Changed\n        from Changed\n          id = $eventSourceId', DiagnosticCodes.OutboundTranslationOutput],
    ['Translate', 'direction outbound\n      projection Publisher => Arrived\n        from Changed\n          id = $eventSourceId', DiagnosticCodes.ForeignPublicEventProduced],
    ['Translate', 'direction outbound\n      projection Publisher => Published\n        from Published\n          id = $eventSourceId', DiagnosticCodes.OutboundTranslationInput],
    ['Translate', 'direction outbound\n      projection Publisher => Published\n        from Arrived\n          id = $eventSourceId', DiagnosticCodes.OutboundTranslationInput],
    ['Translate', 'direction outbound\n      projection First => Published\n        from Changed\n          id = $eventSourceId\n      projection Other => Second\n        from Changed\n          id = $eventSourceId', DiagnosticCodes.OutboundPublicEventCount],
    ['Translate', 'capture Feed\n        source events\n          from Arrived\n        key id', DiagnosticCodes.EventsSourceOutsideInboundTranslation],
    ['Automation', 'capture Feed\n        source events\n          from Arrived\n        key id', DiagnosticCodes.EventsSourceOutsideInboundTranslation],
    ['Translate', 'direction inbound\n      capture Feed\n        source events\n          from Changed\n        key id\n        append Changed', DiagnosticCodes.InboundTranslationInput],
    ['Translate', 'direction inbound\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Published', DiagnosticCodes.InboundTranslationOutput],
    ['Translate', 'direction inbound\n      capture Feed\n        source events\n        key id', DiagnosticCodes.InvalidCaptureEventsSource],
    ['Translate', 'direction inbound\n      capture Feed\n        source events\n          route /x\n        key id', DiagnosticCodes.InvalidCaptureEventsSource],
    ['Translate', 'direction inbound\n      capture Feed\n        source events\n          from Arrived\n          from Arrived\n        key id', DiagnosticCodes.InvalidCaptureEventsSource]
] as const;

describe('when translating through events', () => {
    for (const [kind, body, code] of cases) {
        it(`should report ${code} for ${kind} ${body}`, () => {
            const result = parse(contracts + `    slice ${kind} Transfer\n      ${body}\n`);
            result.diagnostics.some(diagnostic => diagnostic.code === code && diagnostic.severity === 'error').should.be.true;
        });
    }

    it('should accept an event-target projection in an outbound translation', () => {
        parse(contracts + outbound).diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
    });

    it('should accept an event-target reducer in an outbound translation', () => {
        parse(contracts + '    slice Translate Publish\n      direction outbound\n      reducer Publisher => Published\n        on Changed\n          file Reduce.cs\n')
            .diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
    });

    it('should accept events source in an inbound translation and expose the consumed events', () => {
        const result = parse(contracts + '    slice Translate Track\n      direction inbound\n      event Received\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Received\n');
        result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
        const slice = result.value!.modules[0].features[0].slices.at(-1)!;
        consumedEvents(slice.captures[0].source!).map(setting => setting.value).should.deep.equal(['Arrived']);
    });

    it('should keep a read model named like its target a read model', () => {
        const result = parse(contracts + '    slice StateView Orders\n      readmodel Changed\n      projection Changed => Changed\n        from Changed\n          id = $eventSourceId\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(DiagnosticCodes.EventTargetOutsideOutboundTranslation);
    });
});
