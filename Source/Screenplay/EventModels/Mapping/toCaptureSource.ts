// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CaptureSyntax } from '@cratis/screenplay-compiler';
import { AutomationTriggerDocument, AutomationTriggerType, ExternalEventItemDocument } from '../Document/EventModelDocument';
import { SliceScope } from './SliceScope';

// What a translation reads, as the board shows something from outside the model: the system a capture
// reads from when it names one, else the capture itself.
export function toCaptureSource(capture: CaptureSyntax, scope: SliceScope): ExternalEventItemDocument {
    const system = capture.source?.settings.find(setting => setting.name === capture.source?.syntaxKind)?.value;
    return { id: scope.idOf('captures', capture.name), name: system !== undefined && system.length > 0 ? system : capture.name };
}

// What sets a capture off: reading its source, which the board shows as a trigger of its own.
export function toCaptureTrigger(capture: CaptureSyntax): AutomationTriggerDocument {
    const settings = capture.source?.settings.map(setting => `${setting.name} ${setting.value}`).join(', ');
    const from = capture.source === null ? '' : ` from ${capture.source.syntaxKind}${settings === undefined || settings.length === 0 ? '' : ` (${settings})`}`;
    return { type: AutomationTriggerType.custom, description: `Captures ${capture.name}${from}` };
}
