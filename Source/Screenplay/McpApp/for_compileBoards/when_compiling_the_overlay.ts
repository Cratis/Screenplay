// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileBoards } from '../compileBoards';
import { scopeSelectionSource } from '../../Views/for_DependencyMapView/given/scope_selection';

describe('when compiling the overlay for current and proposed boards', () => {
    const boards = compileBoards({ application: 'Work', documents: [{ path: 'work.play', source: scopeSelectionSource }], changes: [{ path: 'work.play', source: scopeSelectionSource.replaceAll('Views', 'ProposedViews') }] });
    it('should find the dependencies of the current model', () => boards.current.overlay.scopes.some(scope => scope.kind === 'module' && scope.dependencies.length === 1 && scope.links.some(link => link.crossing)).should.be.true);
    it('should keep its own overlay for the proposed model', () => boards.proposed!.overlay.scopes.map(scope => scope.id).should.not.deep.equal(boards.current.overlay.scopes.map(scope => scope.id)));
});
