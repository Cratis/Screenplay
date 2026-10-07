// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileBoards } from '../compileBoards';

const documents = [
    { path: 'application.play', source: 'system Mailer\nimport "Projects/module.play"\n' },
    { path: 'Projects/module.play', source: 'module Projects\n  import "Registration/feature.play"\n' },
    { path: 'Projects/Registration/feature.play', source: 'feature Registration\n  import "barrel.play"\n' },
    { path: 'Projects/Registration/barrel.play', source: 'import "Register.play"\nimport "Intent.play"\n' },
    { path: 'Projects/Registration/Intent.play', source: 'slice StateChange Intent\n  operation Send\n    uses Mailer\n    compensate\n  event Recorded\n' },
    { path: 'Projects/Registration/Register.play', source: 'slice StateChange Register\n  command Register\n    produces Recorded\n    produces Send\n' },
];

const slices = (board: ReturnType<typeof compileBoards>['current']) =>
    board.document.collections.flatMap(collection => collection.modules).flatMap(module => module.features).flatMap(feature => feature.slices);

describe('when sketching an imported multifile application', () => {
    for (const selected of documents) {
        it(`should retain the assembled board when sketching ${selected.path}`, () => {
            const boards = compileBoards({ application: 'Projects', documents, changes: [{ ...selected, source: `${selected.source}\n// sketch\n` }] });
            boards.current.errors.should.equal(0);
            boards.proposed!.errors.should.equal(0);
            slices(boards.current).map(slice => slice.name).should.have.members(['Intent', 'Register']);
            slices(boards.proposed!).map(slice => slice.id).should.deep.equal(slices(boards.current).map(slice => slice.id));
        });
    }

    it('should draw both current and proposed boards in authored import order', () => {
        const boards = compileBoards({ application: 'Projects', documents, changes: [{ path: 'Projects/Registration/barrel.play', source: 'import "Intent.play"\nimport "Register.play"\n' }] });
        slices(boards.current).map(slice => slice.name).should.deep.equal(['Register', 'Intent']);
        slices(boards.proposed!).map(slice => slice.name).should.deep.equal(['Intent', 'Register']);
        slices(boards.current).map(slice => slice.sortOrder).should.deep.equal([0, 1]);
        slices(boards.proposed!).map(slice => slice.sortOrder).should.deep.equal([0, 1]);
    });

    it('should draw a proposed new file even when no root imports it', () => {
        const boards = compileBoards({ application: 'Projects', documents, changes: [{ path: 'New.play', source: 'module New\n  feature New\n    slice StateChange Added' }] });
        slices(boards.current).map(slice => slice.name).should.deep.equal(['Register', 'Intent']);
        slices(boards.proposed!).map(slice => slice.name).should.deep.equal(['Register', 'Intent', 'Added']);
    });

    it('should use shared event and operation declarations without inventing operation event cards', () => {
        const boards = compileBoards({ application: 'Projects', documents });
        const register = slices(boards.current).find(slice => slice.name === 'Register')!;
        register.events.should.deep.equal([]);
        slices(boards.current).flatMap(slice => slice.events).map(event => event.name).should.deep.equal(['Recorded']);
        register.command!.logicDescription.should.contain('1. Event: Recorded');
        register.command!.logicDescription.should.contain('2. Operation: Send');
        register.command!.logicDescription.should.contain('Uses Mailer');
        register.command!.logicDescription.should.contain('compensate: pending');
    });

    it('should expose missing imports as errors rather than a verified partial board', () => {
        const boards = compileBoards({ application: 'Projects', documents, changes: [{ path: 'Projects/module.play', source: 'module Projects\n  import "missing.play"\n' }] });
        boards.current.errors.should.equal(0);
        (boards.proposed!.errors > 0).should.be.true;
    });

    it('should expose conflicting placements rather than silently selecting a feature', () => {
        const boards = compileBoards({ application: 'Projects', documents, changes: [{ path: 'Projects/module.play', source: 'module Projects\n  feature F\n    import "Registration/barrel.play"\n  feature G\n    import "Registration/barrel.play"\n' }] });
        boards.current.errors.should.equal(0);
        (boards.proposed!.errors > 0).should.be.true;
    });
});
