// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CaptureSourceSettingSyntax, CaptureSourceSyntax } from './Captures';

/** The source kind of a capture that reads the public events of another application. */
export const eventsSourceKind = 'events';

/** The setting that names one consumed event. */
export const fromSetting = 'from';

/** Gets whether a capture source is `source events`. */
export function isEventsSource(source: CaptureSourceSyntax): boolean {
    return source.syntaxKind === eventsSourceKind;
}

/** Gets the `from <Event>` settings of an events source, in source order; empty for any other source. */
export function consumedEvents(source: CaptureSourceSyntax): readonly CaptureSourceSettingSyntax[] {
    return isEventsSource(source) ? source.settings.filter(setting => setting.name === fromSetting) : [];
}
