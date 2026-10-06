// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationCompilation, discoverImports, inMemoryDocumentSource, matchesPlayPattern, normalizePlayPath, parseFolder, PlayFileSource, recordAuthoredOrder, resolveImports, resolvePlayPattern } from '@cratis/screenplay-compiler';

// Always compile every folder document in the original path order. A root supplies presentation ranks
// only; it must not change duplicate winners, diagnostics, event ownership or which files are drawn.
// VS Code supplies application.play explicitly; the MCP App may discover another importing root.
export function compileEventModelApplication(files: readonly PlayFileSource[], orderingRoot?: string): ApplicationCompilation {
    const compilation = parseFolder(files);
    const documents = new Map(files.map(file => [normalizePlayPath(file.path), file.source]));
    const root = orderingRoot ?? (documents.has('application.play') ? 'application.play' : importingRoot(documents));
    if (root !== undefined && discoverImports(documents.get(root) ?? '', root).length > 0) {
        const resolved = resolveImports([root], inMemoryDocumentSource(documents));
        recordAuthoredOrder(compilation.value, [root], resolved.documents);
    }
    return compilation;
}

function importingRoot(documents: ReadonlyMap<string, string>): string | undefined {
    const importers = new Set<string>();
    const imported = new Set<string>();
    for (const [path, source] of documents) {
        const imports = discoverImports(source, path);
        if (imports.length > 0) importers.add(path);
        for (const item of imports) {
            const pattern = resolvePlayPattern(path, item.fileImport.pattern);
            for (const target of documents.keys()) {
                if (target !== path && matchesPlayPattern(pattern, target)) imported.add(target);
            }
        }
    }
    const roots = [...importers].filter(path => !imported.has(path));
    return roots.length === 1 ? roots[0] : undefined;
}
