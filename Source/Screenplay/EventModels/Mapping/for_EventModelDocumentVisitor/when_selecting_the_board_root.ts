// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileEventModelApplication } from '../compileEventModelApplication';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const names = (files: { path: string; source: string }[]) => {
    const application = compileEventModelApplication(files);
    return toEventModelDocument(application.value, 'Board').collections.flatMap(collection => collection.modules).map(module => module.name);
};

describe('when selecting the board root', () => {
    it('should prefer application.play and exclude unimported scratch documents', () => {
        names([
            { path: 'scratch.play', source: 'module Scratch' },
            { path: 'application.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ]).should.deep.equal(['Z', 'A']);
    });

    it('should find a differently named importing root through nested imports', () => {
        names([
            { path: 'story.play', source: 'import "barrel.play"' },
            { path: 'barrel.play', source: 'import "z.play"\nimport "a.play"' },
            { path: 'a.play', source: 'module A' },
            { path: 'z.play', source: 'module Z' },
        ]).should.deep.equal(['Z', 'A']);
    });

    it('should fall back to path order without an importing root', () => {
        names([{ path: 'z.play', source: 'module Z' }, { path: 'a.play', source: 'module A' }]).should.deep.equal(['A', 'Z']);
        names([]).should.deep.equal([]);
    });

    it('should fall back rather than guess between independent importing roots', () => {
        names([
            { path: 'z.play', source: 'module Z\nimport "shared.play"' },
            { path: 'a.play', source: 'module A\nimport "shared.play"' },
            { path: 'shared.play', source: 'concept Name : String' },
        ]).should.deep.equal(['A', 'Z']);
    });
});
