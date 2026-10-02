// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../../../Diagnostics/Diagnostic';
import { inMemoryDocumentSource, PlacedPlayDocument } from '../../PlayDocumentSource';
import { resolveImports } from '../../PlayImports';
import { PlayPlacement } from '../../PlayPlacement';

// A folder of documents held in memory, resolved from the roots a spec names.
export class a_folder {
    readonly documents = new Map<string, string>();
    resolved: readonly PlacedPlayDocument[] = [];
    diagnostics: readonly Diagnostic[] = [];

    resolve(...roots: string[]): void {
        ({ documents: this.resolved, diagnostics: this.diagnostics } = resolveImports(roots, inMemoryDocumentSource(this.documents)));
    }

    placementOf(path: string): PlayPlacement {
        const document = this.resolved.find(each => each.path === path);
        if (document === undefined) throw new Error(`'${path}' was not resolved`);
        return document.placement;
    }
}
