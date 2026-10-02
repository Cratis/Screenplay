// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileBoards } from '../compileBoards';

const source = `module Projects
  feature Registration
    slice StateChange RegisterProject
      command RegisterProject
        name String
        produces ProjectRegistered
          name = name
      event ProjectRegistered
        name String
`;

const added = `module Projects
  feature Registration
    slice StateChange RenameProject
      command RenameProject
        name String
        produces ProjectRenamed
          name = name
      event ProjectRenamed
        name String
`;

const sliceNames = (document: ReturnType<typeof compileBoards>['current']['document']) =>
    document.collections.flatMap(collection => collection.modules).flatMap(module => module.features).flatMap(feature => feature.slices).map(slice => slice.name);

describe('when compiling the application as it is', () => {
    const boards = compileBoards({ application: 'Projects', documents: [{ path: 'projects.play', source }] });

    it('should draw the application', () => sliceNames(boards.current.document).should.deep.equal(['RegisterProject']));
    it('should have nothing proposed', () => (boards.proposed === undefined).should.be.true);
    it('should find no errors', () => boards.current.errors.should.equal(0));
});

describe('when compiling a change over the application', () => {
    const boards = compileBoards({ application: 'Projects', documents: [{ path: 'projects.play', source }], changes: [{ path: 'renaming.play', source: added }] });

    it('should draw the application as it is', () => sliceNames(boards.current.document).should.deep.equal(['RegisterProject']));
    it('should draw what the change would make of it', () => sliceNames(boards.proposed!.document).should.have.members(['RegisterProject', 'RenameProject']));
    it('should keep the identity of what the change leaves in place', () =>
        boards.proposed!.document.collections[0].modules[0].id.should.equal(boards.current.document.collections[0].modules[0].id));
});
