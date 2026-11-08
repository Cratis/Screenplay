// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { SinkKind, SinkSyntax } from '../Syntax/Sinks';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

// Holds the named sinks of an application to the read models and graphs that choose them - the port of the C#
// SinkValidator. Names are unique ignoring case; a directive names its sink exactly as declared.
export function validateSinks(application: ApplicationSyntax, context: ParserContext): void {
    const sinks = new Map<string, SinkSyntax>();
    const seen = new Map<string, SinkSyntax>();
    for (const sink of application.sinks ?? []) {
        const first = seen.get(sink.name.toLowerCase());
        if (first !== undefined) {
            context.error(DiagnosticCodes.DuplicateSink, `Sink '${sink.name}' is declared more than once; sink names are unique ignoring case`, sink.location);
            continue;
        }
        seen.set(sink.name.toLowerCase(), sink);
        sinks.set(sink.name, sink);
    }
    const used = new Set<string>();
    const check = (name: string, kind: SinkKind, owner: string, location: SourceLocation): void => {
        const sink = sinks.get(name);
        if (sink === undefined) {
            context.error(DiagnosticCodes.UnknownSink, `${owner} names unknown sink '${name}' - declare it with 'sink ${name} for ${keyword(kind)}'`, location);
            return;
        }
        used.add(sink.name);
        if (sink.stores !== kind) {
            context.error(DiagnosticCodes.SinkKindMismatch, `${owner} names sink '${name}', which is declared 'for ${keyword(sink.stores)}'`, location);
        }
    };
    for (const { slice } of new AuthoringProductionResolver(application).slices) {
        for (const readModel of slice.readModels) {
            if (readModel.sink !== undefined) check(readModel.sink, 'ReadModels', `Read model '${readModel.name}'`, readModel.directiveLocations?.sink ?? readModel.location);
        }
        for (const graph of slice.graphs ?? []) {
            if (graph.sink !== undefined) check(graph.sink, 'Graphs', `Graph '${graph.name}'`, graph.directiveLocations?.sink ?? graph.location);
        }
    }
    for (const sink of sinks.values()) {
        if (!used.has(sink.name)) {
            context.warning(DiagnosticCodes.UnusedSink, `Sink '${sink.name}' is not named by any ${describe(sink.stores)}`, sink.location);
        }
    }
}

const keyword = (kind: SinkKind): string => kind === 'Graphs' ? 'graphs' : 'readmodels';

const describe = (kind: SinkKind): string => kind === 'Graphs' ? 'graph' : 'read model';
