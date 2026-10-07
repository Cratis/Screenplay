// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationCompilation, compileApplication, discoverImports, normalizePlayPath, PlayFileSource } from '@cratis/screenplay-compiler';

// Always compile every folder document in the original path order. The compiler's ordering-root helper
// supplies presentation ranks only. A board with application.play does not search for another root.
export function compileEventModelApplication(files: readonly PlayFileSource[], orderingRoot?: string): ApplicationCompilation {
    const documents = new Map(files.map(file => [normalizePlayPath(file.path), file.source]));
    const root = orderingRoot ?? (documents.has('application.play') ? 'application.play' : undefined);
    const presentationRoot = root !== undefined && discoverImports(documents.get(root) ?? '', root).length === 0 ? null : root;
    return compileApplication(documents, undefined, undefined, presentationRoot);
}
