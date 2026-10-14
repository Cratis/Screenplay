// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { consumedEvents, isEventsSource } from '../Syntax/CaptureEventsSource';
import { EventSyntax } from '../Syntax/Declarations';
import { EventVisibility } from '../Syntax/EventVisibility';
import { SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validateCaptureEventFixtures(slice: SliceSyntax, scope: readonly string[], events: AuthoringProductionResolver, context: ParserContext): void {
    const captures = slice.captures.filter(capture => capture.source != null && isEventsSource(capture.source));
    for (const capture of captures) {
        for (const setting of consumedEvents(capture.source!)) {
            const resolved = events.resolveFromScope(setting.value, scope).declaration?.node;
            if (resolved?.kind !== 'EventSyntax') continue;
            const contract = resolved as EventSyntax;
            if (contract.visibility !== EventVisibility.Public || contract.origin == null) continue;
            if (contract.properties.some(property => property.name === capture.key && !property.type.isOptional) && !capture.key?.startsWith('$')) continue;
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
