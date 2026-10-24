// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { consumedEvents, isEventsSource } from '../Syntax/CaptureEventsSource';
import { CaptureSyntax } from '../Syntax/Captures';
import { EventSyntax } from '../Syntax/Declarations';
import { EventVisibility } from '../Syntax/EventVisibility';
import { SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validateEventSourceKeys(slice: SliceSyntax, context: ParserContext): void {
    for (const capture of slice.captures.filter(isMisplacedEventSourceKey)) {
        context.error(DiagnosticCodes.CaptureEventSourceKeyOutsideEvents, `Capture '${capture.name}' declares 'is event source' on its key; only a 'source events' capture with a single property key can.`, capture.location);
    }
}

function isMisplacedEventSourceKey(capture: CaptureSyntax): boolean {
    return capture.keyIsEventSource === true && !isSingleKeyEventsCapture(capture);
}

function isSingleKeyEventsCapture(capture: CaptureSyntax): boolean {
    return capture.source != null && isEventsSource(capture.source) && isPropertyKey(capture.key);
}

function isPropertyKey(key: string | null): boolean {
    return key != null && key !== '' && !key.startsWith('$') && !key.includes('.');
}

export function validateCaptureEventFixtures(slice: SliceSyntax, scope: readonly string[], events: AuthoringProductionResolver, context: ParserContext): void {
    const captures = slice.captures.filter(capture => capture.source != null && isEventsSource(capture.source));
    for (const capture of captures) {
        if (capture.key?.startsWith('$')) {
            context.error(DiagnosticCodes.CaptureEventsKeyNotRequired, `Capture '${capture.name}' key '${capture.key}' must be a required property of every source event; context keys are not supported for captures over events.`, capture.location);
            continue;
        }
        for (const setting of consumedEvents(capture.source!)) {
            const resolved = events.resolveFromScope(setting.value, scope).declaration?.node;
            if (resolved?.kind !== 'EventSyntax') continue;
            const contract = resolved as EventSyntax;
            if (contract.visibility !== EventVisibility.Public || contract.origin == null) continue;
            if (contract.properties.some(property => property.name === capture.key && !property.type.isOptional)) continue;
            context.error(DiagnosticCodes.CaptureEventsKeyNotRequired, `Capture '${capture.name}' key '${capture.key}' must be a required property of every source event; '${contract.name}' does not declare it as required.`, capture.location);
            break;
        }
    }
    const declarations = events.slices.flatMap(item => item.slice.captures);
    for (const specification of slice.specifications) {
        const records = [...specification.givenCaptures, ...(specification.whenCapture == null ? [] : [specification.whenCapture])];
        for (const record of records) {
            const matches = declarations.filter(capture => capture.name === record.capture.split('.').at(-1));
            if (matches.length !== 1 || matches[0].source == null || !isEventsSource(matches[0].source)) continue;
            context.error(DiagnosticCodes.CaptureEventsRecordFixture, `Capture '${record.capture}' consumes events; use 'given <Event>' or 'when append <Event>' rather than a capture record.`, record.location);
        }
    }
}
