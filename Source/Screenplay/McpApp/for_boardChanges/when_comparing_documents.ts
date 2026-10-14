// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { boardChanges } from '../boardChanges';
import { compileBoards } from '../compileBoards';

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

const compare = (current: string, proposed: string) => {
    const boards = compileBoards({ application: 'Projects', documents: [{ path: 'projects.play', source: current }], changes: [{ path: 'projects.play', source: proposed }] });
    return { boards, sets: boardChanges(boards.current.document, boards.proposed!.document) };
};
const idOf = (document: { collections: { modules: { id: string; features: { id: string; slices: { id: string; command?: { id: string } }[] }[] }[] }[] }) => document.collections[0].modules[0];

describe('when the proposal adds a slice', () => {
    const { boards, sets } = compare(module('Projects', slice('Register')), module('Projects', slice('Register'), slice('Rename')));
    const added = idOf(boards.proposed!.document).features[0].slices[1];

    it('should mark the slice added', () => sets.proposed.changes.get(added.id)!.should.equal('added'));
    it('should mark its command added', () => sets.proposed.changes.get(added.command!.id)!.should.equal('added'));
    it('should count the slice, command and event', () => sets.proposed.added.should.equal(3));
    it('should mark nothing changed', () => sets.proposed.changed.should.equal(0));
    it('should mark nothing on the current model', () => sets.current.changes.size.should.equal(0));
    it('should open at the slice before what it holds', () => sets.proposed.first!.should.equal(added.id));
});

describe('when the proposal removes a slice', () => {
    const { boards, sets } = compare(module('Projects', slice('Register'), slice('Rename')), module('Projects', slice('Register')));
    const removed = idOf(boards.current.document).features[0].slices[1];

    it('should mark the slice removed on the current model', () => sets.current.changes.get(removed.id)!.should.equal('removed'));
    it('should count the slice, command and event', () => sets.current.removed.should.equal(3));
    it('should mark nothing on the proposed model', () => sets.proposed.changes.size.should.equal(0));
    it('should open the current model at the slice', () => sets.current.first!.should.equal(removed.id));
});

describe('when the proposal changes a field of a command', () => {
    const { boards, sets } = compare(module('Projects', slice('Register', 'name')), module('Projects', slice('Register', 'title')));
    const changed = idOf(boards.proposed!.document).features[0].slices[0];

    it('should mark the command changed', () => sets.proposed.changes.get(changed.command!.id)!.should.equal('changed'));
    it('should not mark the slice, which holds the command', () => sets.proposed.changes.has(changed.id).should.be.false);
    it('should not mark the feature', () => sets.proposed.changes.has(idOf(boards.proposed!.document).features[0].id).should.be.false);
    it('should not mark the module', () => sets.proposed.changes.has(idOf(boards.proposed!.document).id).should.be.false);
    it('should mark nothing added or removed', () => `${sets.proposed.added}${sets.current.removed}`.should.equal('00'));
});

describe('when the proposal changes only what layout is made of', () => {
    const { boards } = compare(module('Projects', slice('Register')), module('Projects', slice('Register')));
    const collapsed = structuredClone(boards.proposed!.document);
    collapsed.collections[0].modules[0].collapsed = true;
    collapsed.collections[0].modules[0].sortOrder = 7;
    collapsed.collections[0].modules[0].features[0].slices[0].collapsed = true;
    const sets = boardChanges(boards.current.document, collapsed);

    it('should mark nothing', () => sets.proposed.changes.size.should.equal(0));
    it('should open nowhere', () => (sets.proposed.first === undefined).should.be.true);
});

describe('when the proposal changes something in a later module', () => {
    const { boards, sets } = compare(
        module('Projects', slice('Register')) + module('Billing', slice('Invoice', 'number')),
        module('Projects', slice('Register')) + module('Billing', slice('Invoice', 'reference')));
    const billing = boards.proposed!.document.collections[0].modules[1];

    it('should open at the first change in board order', () => sets.proposed.first!.should.equal(billing.features[0].slices[0].command!.id));
});

describe('when the proposal adds in two places', () => {
    const { boards, sets } = compare(
        module('Projects', slice('Register')) + module('Billing', slice('Invoice')),
        module('Projects', slice('Register'), slice('Rename')) + module('Billing', slice('Invoice'), slice('Credit')));

    it('should open at the first one the board draws', () => sets.proposed.first!.should.equal(boards.proposed!.document.collections[0].modules[0].features[0].slices[1].id));
});

describe('when there is no proposal', () => {
    const boards = compileBoards({ application: 'Projects', documents: [{ path: 'projects.play', source: module('Projects', slice('Register')) }] });
    const sets = boardChanges(boards.current.document, undefined);

    it('should mark nothing on the current model', () => sets.current.changes.size.should.equal(0));
    it('should mark nothing on the proposed model', () => sets.proposed.changes.size.should.equal(0));
    it('should open nowhere', () => (sets.current.first === undefined && sets.proposed.first === undefined).should.be.true);
});

describe('when the proposal is the application as it is', () => {
    const source = module('Projects', slice('Register'));
    const { sets } = compare(source, source);

    it('should mark nothing', () => (sets.proposed.changes.size + sets.current.changes.size).should.equal(0));
});
