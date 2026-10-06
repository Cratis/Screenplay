// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationCompilation, compileApplication, discoverImports, matchesPlayPattern, normalizePlayPath, parseFolder, PlayFileSource, resolvePlayPattern } from '@cratis/screenplay-compiler';

// Boards follow application.play, or the one importing document that is not itself imported. Without an
// unambiguous root the folder remains an ordinal-path compilation, just as before.
export function compileEventModelApplication(files: readonly PlayFileSource[]): ApplicationCompilation {
    const documents = new Map(files.map(file => [normalizePlayPath(file.path), file.source]));
    if (documents.has('application.play')) return compileApplication(documents, ['application.play']);
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
    return roots.length === 1 ? compileApplication(documents, roots) : parseFolder(files);
}
