// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { CompilationResult, parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { SyntaxJsonValue, toSyntaxJson } from '../../Syntax/SyntaxJson';
import { parseFolder, PlayFileSource } from '../PlayApplicationAssembly';

const corpus = resolve(__dirname, '../../../../DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v2/source');

function filesIn(folder: string, root = folder): PlayFileSource[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        return statSync(path).isDirectory() ? filesIn(path, root) : [{ path: relative(root, path), source: readFileSync(path, 'utf8') }];
    });
}

// A folder is read in ordinal order of its paths, so its slices come in that order rather than in the order
// the single document writes them - the C# compiler does the same.
function slicesByName(value: SyntaxJsonValue): SyntaxJsonValue {
    if (Array.isArray(value)) {
        return value.map(slicesByName);
    }
    if (value === null || typeof value !== 'object') {
        return value;
    }
    const result = Object.fromEntries(Object.entries(value).map(([member, child]) => [member, slicesByName(child)]));
    if (Array.isArray(result.slices)) {
        result.slices = [...result.slices].sort((left, right) => String((left as { name: string }).name).localeCompare(String((right as { name: string }).name)));
    }
    return result;
}

// The canonical corpus states one model twice: as a single document and split across a folder. Merged,
// the folder has to be the single document.
describe('when parsing the canonical folder', () => {
    let files: PlayFileSource[];
    let folder: CompilationResult<ApplicationSyntax>;
    let single: CompilationResult<ApplicationSyntax>;

    beforeAll(() => {
        files = filesIn(join(corpus, 'folder'));
        folder = parseFolder(files);
        single = parse(readFileSync(join(corpus, 'RegisterProject.play'), 'utf8'));
    });

    it('should read every document of the folder', () => {
        files.length.should.equal(5);
    });

    it('should succeed', () => {
        folder.diagnostics.filter(diagnostic => diagnostic.code !== 'PLAY0479').should.deep.equal([]);
    });

    it('should merge into the same syntax as the single document', () => {
        (slicesByName(toSyntaxJson(folder.value)) as object).should.deep.equal(slicesByName(toSyntaxJson(single.value)));
    });

    it('should keep where each slice came from, in folder order', () => {
        folder.value.modules[0].features[0].slices.map(slice => slice.location.path ?? '').should.deep.equal([
            join('Projects', 'Registration', 'ProjectLookup', 'ProjectLookup.play'),
            join('Projects', 'Registration', 'RegisterProject', 'RegisterProject.play'),
        ]);
    });
});
