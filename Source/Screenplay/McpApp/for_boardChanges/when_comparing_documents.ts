// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { EventModelDocument } from '@cratis/screenplay-event-models';
import { boardChanges } from '../boardChanges';
import { compileBoards } from '../compileBoards';
import type { BoardChangeSets } from '../BoardChangeSets';

const slice = (name: string, field = 'name') => `    slice StateChange ${name}
      command ${name}
        ${field} String
        produces ${name}d
          ${field} = ${field}
      event ${name}d
        ${field} String
`;

const module = (name: string, ...slices: string[]) => `module ${name}
  feature Registration
${slices.join('')}`;

const compile = (current: string, proposed?: string) => compileBoards({
    application: 'Projects',
    documents: [{ path: 'projects.play', source: current }],
    changes: proposed === undefined ? undefined : [{ path: 'projects.play', source: proposed }],
});
const firstModule = (document: EventModelDocument) => document.collections[0].modules[0];
const firstSlice = (document: EventModelDocument) => firstModule(document).features[0].slices[0];

describe('when the proposal adds a slice', () => {
    let sets: BoardChangeSets;
    let added: ReturnType<typeof firstSlice>;

    beforeEach(() => {
        const boards = compile(module('Projects', slice('Register')), module('Projects', slice('Register'), slice('Rename')));
        sets = boardChanges(boards.current.document, boards.proposed!.document);
        added = firstModule(boards.proposed!.document).features[0].slices[1];
    });

    it('should mark the slice added', () => sets.proposed.changes.get(added.id)!.should.equal('added'));
    it('should mark its command added', () => sets.proposed.changes.get(added.command!.id)!.should.equal('added'));
    it('should count the slice, command and event', () => sets.proposed.added.should.equal(3));
    it('should mark nothing changed', () => sets.proposed.changed.should.equal(0));
    it('should mark nothing on the current model', () => sets.current.changes.size.should.equal(0));
    it('should open at the slice before what it holds', () => sets.proposed.first!.should.equal(added.id));
});

describe('when the proposal removes a slice', () => {
    let sets: BoardChangeSets;
    let removed: ReturnType<typeof firstSlice>;

    beforeEach(() => {
        const boards = compile(module('Projects', slice('Register'), slice('Rename')), module('Projects', slice('Register')));
        sets = boardChanges(boards.current.document, boards.proposed!.document);
        removed = firstModule(boards.current.document).features[0].slices[1];
    });

    it('should mark the slice removed on the current model', () => sets.current.changes.get(removed.id)!.should.equal('removed'));
    it('should count the slice, command and event', () => sets.current.removed.should.equal(3));
    it('should mark nothing on the proposed model', () => sets.proposed.changes.size.should.equal(0));
    it('should open the current model at the slice', () => sets.current.first!.should.equal(removed.id));
});

describe('when the proposal changes a field of a command', () => {
    let sets: BoardChangeSets;
    let proposed: EventModelDocument;

    beforeEach(() => {
        const boards = compile(module('Projects', slice('Register', 'name')), module('Projects', slice('Register', 'title')));
        proposed = boards.proposed!.document;
        sets = boardChanges(boards.current.document, proposed);
    });

    it('should mark the command changed', () => sets.proposed.changes.get(firstSlice(proposed).command!.id)!.should.equal('changed'));
    it('should not mark the slice, which holds the command', () => sets.proposed.changes.has(firstSlice(proposed).id).should.be.false);
    it('should not mark the feature', () => sets.proposed.changes.has(firstModule(proposed).features[0].id).should.be.false);
    it('should not mark the module', () => sets.proposed.changes.has(firstModule(proposed).id).should.be.false);
    it('should mark nothing added or removed', () => `${sets.proposed.added}${sets.current.removed}`.should.equal('00'));
});

describe('when the proposal changes only what layout is made of', () => {
    let sets: BoardChangeSets;

    beforeEach(() => {
        const boards = compile(module('Projects', slice('Register')), module('Projects', slice('Register')));
        const collapsed = structuredClone(boards.proposed!.document);
        collapsed.collections[0].modules[0].collapsed = true;
        collapsed.collections[0].modules[0].sortOrder = 7;
        collapsed.collections[0].modules[0].features[0].slices[0].collapsed = true;
        sets = boardChanges(boards.current.document, collapsed);
    });

    it('should mark nothing', () => sets.proposed.changes.size.should.equal(0));
    it('should open nowhere', () => (sets.proposed.first === undefined).should.be.true);
});

describe('when the proposal changes something in a later module', () => {
    let sets: BoardChangeSets;
    let billing: EventModelDocument['collections'][0]['modules'][0];

    beforeEach(() => {
        const boards = compile(
            module('Projects', slice('Register')) + module('Billing', slice('Invoice', 'number')),
            module('Projects', slice('Register')) + module('Billing', slice('Invoice', 'reference')));
        sets = boardChanges(boards.current.document, boards.proposed!.document);
        billing = boards.proposed!.document.collections[0].modules[1];
    });

    it('should open at the first change in board order', () => sets.proposed.first!.should.equal(billing.features[0].slices[0].command!.id));
});

describe('when the proposal adds in two places', () => {
    let sets: BoardChangeSets;
    let expected: string;

    beforeEach(() => {
        const boards = compile(
            module('Projects', slice('Register')) + module('Billing', slice('Invoice')),
            module('Projects', slice('Register'), slice('Rename')) + module('Billing', slice('Invoice'), slice('Credit')));
        sets = boardChanges(boards.current.document, boards.proposed!.document);
        expected = firstModule(boards.proposed!.document).features[0].slices[1].id;
    });

    it('should open at the first one the board draws', () => sets.proposed.first!.should.equal(expected));
});

describe('when there is no proposal', () => {
    let sets: BoardChangeSets;

    beforeEach(() => {
        sets = boardChanges(compile(module('Projects', slice('Register'))).current.document, undefined);
    });

    it('should mark nothing on the current model', () => sets.current.changes.size.should.equal(0));
    it('should mark nothing on the proposed model', () => sets.proposed.changes.size.should.equal(0));
    it('should open nowhere', () => (sets.current.first === undefined && sets.proposed.first === undefined).should.be.true);
});

describe('when the proposal is the application as it is', () => {
    let sets: BoardChangeSets;

    beforeEach(() => {
        const source = module('Projects', slice('Register'));
        const boards = compile(source, source);
        sets = boardChanges(boards.current.document, boards.proposed!.document);
    });

    it('should mark nothing', () => (sets.proposed.changes.size + sets.current.changes.size).should.equal(0));
});

// The board draws a marker on modules, features, slices and their command, read model and event cards only.
// A change in anything else it draws marks the nearest of those that holds it.
describe('when the proposal adds a screen to a slice', () => {
    let sets: BoardChangeSets;
    let target: ReturnType<typeof firstSlice>;

    beforeEach(() => {
        const boards = compile(module('Projects', slice0()));
        const proposed = structuredClone(boards.current.document);
        target = firstSlice(proposed);
        target.actors.push({ id: 'actor-1', type: 0, layout: 0, windowWidth: 800, windowHeight: 600, elements: [{ id: 'screen-1', name: 'RegisterScreen', type: 'panel', width: 100, height: 50, ZIndex: 0, visibility: 0, isEnabled: true, opacity: 1, anchoring: 0, properties: { canvas: { x: 0, y: 0 } } }] });
        sets = boardChanges(boards.current.document, proposed);
    });

    it('should mark the slice changed, since a screen has no marker of its own', () => sets.proposed.changes.get(target.id)!.should.equal('changed'));
    it('should mark only the slice', () => sets.proposed.changes.size.should.equal(1));
    it('should open at the slice', () => sets.proposed.first!.should.equal(target.id));
});

describe('when the proposal adds a query to a slice', () => {
    let sets: BoardChangeSets;
    let target: ReturnType<typeof firstSlice>;

    beforeEach(() => {
        const boards = compile(module('Projects', slice0()));
        const proposed = structuredClone(boards.current.document);
        target = firstSlice(proposed);
        target.queries.push({ id: 'query-1', name: 'AllProjects', parameters: [] });
        sets = boardChanges(boards.current.document, proposed);
    });

    it('should mark the slice changed, since a query has no marker of its own', () => sets.proposed.changes.get(target.id)!.should.equal('changed'));
    it('should mark only the slice', () => sets.proposed.changes.size.should.equal(1));
});

describe('when the proposal adds a persona lane', () => {
    let sets: BoardChangeSets;
    let owner: string;

    beforeEach(() => {
        const boards = compile(module('Projects', slice0()) + module('Billing', slice('Invoice')));
        const proposed = structuredClone(boards.current.document);
        proposed.collections[0].actors.push({ id: 'actor-2', name: 'Manager', actorType: 0, description: '' });
        owner = firstModule(proposed).id;
        sets = boardChanges(boards.current.document, proposed);
    });

    it('should mark the first module of the collection changed, since a lane has no marker of its own', () => sets.proposed.changes.get(owner)!.should.equal('changed'));
    it('should mark only that module', () => sets.proposed.changes.size.should.equal(1));
    it('should open at that module', () => sets.proposed.first!.should.equal(owner));
});

describe('when the proposal removes a persona lane', () => {
    let sets: BoardChangeSets;
    let owner: string;

    beforeEach(() => {
        const boards = compile(module('Projects', slice0()));
        const current = structuredClone(boards.current.document);
        current.collections[0].actors.push({ id: 'actor-2', name: 'Manager', actorType: 0, description: '' });
        owner = firstModule(current).id;
        sets = boardChanges(current, boards.current.document);
    });

    it('should mark the owning module changed on the proposed model', () => sets.proposed.changes.get(owner)!.should.equal('changed'));
});

function slice0() {
    return slice('Register');
}
