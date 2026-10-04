// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from '../Text/patterns';
import { LineReader } from './LineReader';
import { SourceLine } from './SourceLine';

const sourceHeader = pattern('^eventsource\\s+([A-Za-z_]\\w*)$');
const typeHeader = pattern('^(?:type\\s+([A-Za-z_]\\w*)|concept\\s+([A-Za-z_]\\w*)\\s*:.*)$');
const importHeader = pattern('^import\\s+([\\w.]+)$');
const streamHeader = pattern('^stream\\s+([A-Za-z_]\\w*)$');

// One declaration-header capture over the immutable whole input, before any command classification.
// Duplicates are not resolved by encounter order. Fences and non-application declarations are opaque.
export class CommandStreamCandidates {
    private constructor(private readonly sources: ReadonlyMap<string, readonly (readonly string[])[]>, private readonly qualifiedTypes: ReadonlySet<string>) {}

    hasSource(name: string): boolean { return this.sources.has(name); }
    hasPropertyType(name: string): boolean { return this.qualifiedTypes.has(name); }
    hasUniqueStream(source: string, stream: string): boolean {
        const parents = this.sources.get(source);
        return parents?.length === 1 && parents[0].filter(name => name === stream).length === 1;
    }

    static capture(documents: Iterable<readonly SourceLine[]>): CommandStreamCandidates {
        const sources = new Map<string, string[][]>();
        const types = new Set<string>();
        const imports = new Set<string>();
        for (const lines of documents) {
            const reader = new LineReader(lines);
            for (let header = reader.peekSignificant(); header !== undefined; header = reader.peekSignificant()) {
                reader.takeSignificant();
                const source = sourceHeader.exec(header.content);
                const type = typeHeader.exec(header.content);
                const imported = importHeader.exec(header.content);
                const streams: string[] = [];
                if (type !== null) types.add(type[1] ?? type[2]);
                if (imported !== null) imports.add(imported[1]);
                for (let child = reader.peekSignificant(); child !== undefined && child.indent > header.indent; child = reader.peekSignificant()) {
                    reader.takeSignificant();
                    const stream = streamHeader.exec(child.content);
                    if (source !== null && stream !== null) streams.push(stream[1]);
                    skipChildren(reader, child);
                }
                if (source !== null) sources.set(source[1], [...sources.get(source[1]) ?? [], streams]);
            }
        }
        const qualified = new Set([...imports].filter(name => types.has(name.substring(name.lastIndexOf('.') + 1))));
        return new CommandStreamCandidates(sources, qualified);
    }
}

function skipChildren(reader: LineReader, header: SourceLine): void {
    const skipFence = () => { for (let raw = reader.takeRaw(); raw !== undefined && raw.raw.trim() !== '```'; raw = reader.takeRaw()) { /* opaque fence */ } };
    if (header.content.startsWith('```')) { skipFence(); return; }
    for (let child = reader.peekSignificant(); child !== undefined && child.indent > header.indent; child = reader.peekSignificant()) {
        reader.takeSignificant();
        if (child.content.startsWith('```')) skipFence();
    }
}
