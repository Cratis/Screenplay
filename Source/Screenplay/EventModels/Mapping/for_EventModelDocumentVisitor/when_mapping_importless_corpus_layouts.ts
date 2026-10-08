// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { basename, dirname, join, relative, resolve } from 'node:path';
import { describe, it } from 'vitest';
import { discoverImports, parseFolder } from '@cratis/screenplay-compiler';
import { compileEventModelApplication } from '../compileEventModelApplication';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const corpus = resolve(__dirname, '../../../../../Source/DotNET/Screenplay.CanonicalCorpus/Corpus');
function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(name => {
        const path = join(folder, name);
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}
const roots = playFiles(corpus).filter(path => basename(path) === 'application.play');

describe('when mapping import-less canonical corpus layouts', () => {
    it('should exercise all six folder-style roots', () => roots.length.should.equal(6));
    for (const root of roots) {
        it(`should preserve the entire board for ${relative(corpus, root)}`, () => {
            discoverImports(readFileSync(root, 'utf8')).should.deep.equal([]);
            const folder = dirname(root);
            const files = playFiles(folder).map(path => ({ path: relative(folder, path), source: readFileSync(path, 'utf8') }));
            const baseline = toEventModelDocument(parseFolder(files).value, 'Corpus');
            baseline.collections.length.should.be.greaterThan(0);
            toEventModelDocument(compileEventModelApplication(files).value, 'Corpus').should.deep.equal(baseline);
        });
    }
});
