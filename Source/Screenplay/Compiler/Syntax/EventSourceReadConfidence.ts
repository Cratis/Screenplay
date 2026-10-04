// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { EventSourceSyntax } from './EventSources';

// Physical source-only read confidence; it never grants editing or executable identity.
export class EventSourceReadConfidence {
    private readonly sources = new Map<string, { source: EventSourceSyntax; placementResolved: boolean }[]>();

    constructor(sources: readonly { source: EventSourceSyntax; placementResolved: boolean }[], private readonly complete: boolean) {
        for (const entry of sources) this.sources.set(entry.source.name, [...this.sources.get(entry.source.name) ?? [], entry]);
    }

    static hasUnknownExtent(diagnostics: readonly Diagnostic[]) {
        return diagnostics.some(diagnostic => diagnostic.severity === 'error' && ['PLAY0001', 'PLAY0503'].includes(diagnostic.code));
    }

    resolve(source: string, stream?: string) {
        const parents = this.sources.get(source) ?? [];
        const streams = stream === undefined ? [] : parents.flatMap(parent => parent.source.streams).filter(child => child.name === stream);
        const ambiguous = parents.length > 1 || streams.length > 1;
        const incomplete = !this.complete || parents.some(parent => !parent.placementResolved);
        const state = ambiguous ? 'ambiguous' : incomplete ? 'incomplete' : parents.length === 1 && (stream === undefined || streams.length === 1) ? 'unique' : 'notFound';
        const reasons: string[] = [];
        if (parents.length > 1) reasons.push('Multiple physical parent sources claim the exact name.');
        if (streams.length > 1) reasons.push('Multiple physical streams claim the exact parent and local name.');
        if (parents.some(parent => !parent.placementResolved)) reasons.push('A physical parent has unresolved import placement.');
        if (!this.complete) reasons.push('Physical source extent or placement is incomplete; missing declarations cannot be excluded.');
        return { state, sources: parents.map(parent => parent.source), streams, reasons };
    }
}
