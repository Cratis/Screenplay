// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { documentPlacement, PlayPlacement } from '../Files/PlayPlacement';
import { sourceContext } from './SourceOptionsParser';
import { parseApplication } from './ScreenplayParser';
import { SourceLine } from './SourceLine';

// One noncommitting real parse per immutable document, without stream candidates. Commands retain
// legacy properties; the parser owns leaf/block/fence rules. Capture never calls itself recursively.
export class CommandStreamCandidates {
    private constructor(private readonly sources: ReadonlyMap<string, readonly (readonly string[])[]>, private readonly qualifiedTypes: ReadonlySet<string>) {}

    hasSource(name: string): boolean { return this.sources.has(name); }
    hasPropertyType(name: string): boolean { return this.qualifiedTypes.has(name); }
    hasUniqueStream(source: string, stream: string): boolean {
        const parents = this.sources.get(source);
        return parents?.length === 1 && parents[0].filter(name => name === stream).length === 1;
    }

    static capture(documents: Iterable<readonly SourceLine[]>, placement: PlayPlacement = documentPlacement, languages?: ReadonlySet<string>): CommandStreamCandidates {
        return this.capturePlaced([...documents].map(lines => ({ lines, placement })), languages);
    }

    static capturePlaced(documents: Iterable<{ readonly lines: readonly SourceLine[]; readonly placement: PlayPlacement }>, languages?: ReadonlySet<string>): CommandStreamCandidates {
        const sources = new Map<string, string[][]>();
        const types = new Set<string>();
        const imports = new Set<string>();
        for (const { lines, placement } of documents) {
            const context = sourceContext(lines, lines[0]?.path, languages);
            context.scope = placement;
            const application = parseApplication(context, lines, placement);
            for (const type of application.types) types.add(type.name);
            for (const concept of application.concepts) types.add(concept.name);
            for (const imported of application.imports) imports.add(imported.qualifiedName);
            for (const source of application.eventSources ?? []) {
                sources.set(source.name, [...sources.get(source.name) ?? [], source.streams.map(stream => stream.name)]);
            }
        }
        const qualified = new Set([...imports].filter(name => types.has(name.substring(name.lastIndexOf('.') + 1))));
        return new CommandStreamCandidates(sources, qualified);
    }
}
