// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { inMemoryDocumentSource } from '../PlayDocumentSource';
import { resolveImports } from '../PlayImports';
import { a_folder } from './given/a_folder';

describe('when barrel order differs from discovery order', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'import "Zulu/Zulu.play"\nimport "Alpha/Alpha.play"\n');
        folder.documents.set('Zulu/Zulu.play', 'module Zulu\n  import "Zulu/Zulu.play"\n  import "Alpha/Alpha.play"\n');
        folder.documents.set('Zulu/Zulu/Zulu.play', 'feature Zulu\n  import "Second.play"\n  import "First.play"\n');
        folder.documents.set('Zulu/Zulu/Second.play', 'slice StateView Second\n');
        folder.documents.set('Zulu/Zulu/First.play', 'slice StateView First\n');
        folder.documents.set('Zulu/Alpha/Alpha.play', 'feature Alpha\n');
        folder.documents.set('Alpha/Alpha.play', 'module Alpha\n');
        folder.resolve('application.play');
    });

    it('should resolve without diagnostics', () => {
        folder.diagnostics.should.be.empty;
    });

    it('should keep folder merge order independent of imports', () => {
        const roots = [...folder.documents.keys()].sort();
        resolveImports(roots, inMemoryDocumentSource(folder.documents)).documents.map(document => document.path).should.deep.equal(roots);
    });

    it('should follow authored import order from the single root', () => {
        folder.resolved.map(document => document.path).should.deep.equal([
            'application.play', 'Zulu/Zulu.play', 'Zulu/Zulu/Zulu.play', 'Zulu/Zulu/Second.play',
            'Zulu/Zulu/First.play', 'Zulu/Alpha/Alpha.play', 'Alpha/Alpha.play'
        ]);
    });
});
