// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiscoveredImport } from '../Parsing/ImportDiscovery';
import { discoverImports } from '../ScreenplayCompiler';
import { PlacedPlayDocument } from './PlayDocumentSource';
import { matchesPlayPattern, normalizePlayPath, resolvePlayPattern } from './PlayGlob';

// Choosing presentation order never changes the roots used for resolving or merging documents.
export function selectOrderingRoot(roots: readonly string[], documents: readonly PlacedPlayDocument[], languages?: ReadonlySet<string>, imports?: ReadonlyMap<string, readonly DiscoveredImport[]>): string | undefined {
    const normalized = [...new Set(roots.map(normalizePlayPath))];
    if (normalized.length === 1) return normalized[0];
    imports ??= new Map(documents.map(document => [document.path, discoverImports(document.source, document.path, languages)]));
    if ((imports.get('application.play')?.length ?? 0) > 0) return 'application.play';
    const imported = new Set<string>();
    for (const [path, entries] of imports) {
        for (const entry of entries) {
            const pattern = resolvePlayPattern(path, entry.fileImport.pattern);
            documents.filter(document => document.path !== path && matchesPlayPattern(pattern, document.path)).forEach(document => imported.add(document.path));
        }
    }
    const candidates = [...imports].filter(([path, entries]) => entries.length > 0 && !imported.has(path)).map(([path]) => path);
    return candidates.length === 1 ? candidates[0] : undefined;
}
