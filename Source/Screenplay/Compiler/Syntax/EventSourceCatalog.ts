// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSourceSyntax, EventSourceResolution, EventSourceResolutionKind } from './EventSources';
import { ApplicationSyntax } from './Structure';

export { type EventSourceResolution, EventSourceResolutionKind } from './EventSources';
// Exact application/source/local-name keys only. Paths identify physical occurrences, not semantic ids.
export class EventSourceCatalog {
    private readonly sources = new Map<string, EventSourceSyntax[]>();
    private readonly otherNames: ReadonlySet<string>;

    constructor(application: ApplicationSyntax) {
        for (const source of application.eventSources ?? []) this.sources.set(source.name, [...this.sources.get(source.name) ?? [], source]);
        this.otherNames = new Set([...application.concepts, ...application.types].map(type => type.name));
    }

    resolve(source: string, stream?: string): EventSourceResolution {
        const sources = this.sources.get(source) ?? [];
        const streams = sources.flatMap(parent => parent.streams).filter(candidate => candidate.name === stream);
        let kind: EventSourceResolutionKind;
        if (sources.length === 0) kind = this.otherNames.has(source) ? EventSourceResolutionKind.WrongKind : EventSourceResolutionKind.NotFound;
        else if (sources.length > 1 || streams.length > 1) kind = EventSourceResolutionKind.Ambiguous;
        else kind = stream === undefined || streams.length === 1 ? EventSourceResolutionKind.Unique : EventSourceResolutionKind.NotFound;
        return { kind, sources, streams };
    }
}
