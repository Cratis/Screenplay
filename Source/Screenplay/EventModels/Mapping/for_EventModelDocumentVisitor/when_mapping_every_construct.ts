// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { EventModelDocument, SliceType } from '../../Document/EventModelDocument';
import { userActor } from '../../Prototypes/toUserExperience';
import { ids_in, problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named, the_constructs_document } from './given/the_constructs_document';

describe('when mapping every construct', () => {
    let document: EventModelDocument;

    beforeAll(() => {
        document = the_constructs_document();
    });

    it('should be a document the board can read', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });

    it('should put every module into one collection', () => {
        document.collections.map(collection => collection.modules.map(module => module.name)).should.deep.equal([['Customers']]);
    });

    it('should keep nested features', () => {
        document.collections[0].modules[0].features[0].subFeatures.map(feature => feature.name).should.deep.equal(['Drafts']);
    });

    it('should give every element its own id', () => {
        const ids = ids_in(document);
        new Set(ids).size.should.equal(ids.length);
    });

    it('should draw the screens for the user', () => {
        document.collections[0].actors.should.deep.equal([userActor]);
        slice_named(document, 'CustomerList').actors.map(actor => [actor.id, actor.elements.length > 0]).should.deep.equal([[userActor.id, true]]);
        slice_named(document, 'Register').actors.should.deep.equal([]);
    });

    it('should map the slice types', () => {
        ['Register', 'CustomerList', 'Reminders', 'Mirroring'].map(name => slice_named(document, name).sliceType)
            .should.deep.equal([SliceType.stateChange, SliceType.stateView, SliceType.automation, SliceType.translator]);
    });

    it('should carry the command with the rules the board has an equivalent for', () => {
        slice_named(document, 'Register').command!.rules.map(rules => [rules.propertyName, rules.rules.map(rule => rule.ruleType)]).should.deep.equal([
            ['name', ['NotEmpty', 'MaxLength', 'Length']],
            ['age', ['GreaterThanOrEqual', 'LessThan']],
            ['email', ['Matches']],
        ]);
    });

    it('should shape the command from its properties, resolving concepts', () => {
        (slice_named(document, 'Register').command!.schema.properties as Record<string, unknown>).should.deep.include({
            customerId: { type: 'string', format: 'uuid' },
            age: { type: 'integer' },
            tags: { type: 'array', items: { type: 'string' } },
        });
    });

    it('should carry the constraints on the events they constrain', () => {
        slice_named(document, 'Register').events.map(event => [event.name, event.constraints]).should.deep.equal([
            ['CustomerRegistered', { unique: { name: 'UniqueEmail', message: 'That email is taken' } }],
            ['CustomerArchived', { uniqueEventType: { name: 'OneArchive', message: '' } }],
        ]);
    });

    it('should show a state view the read model its projection builds', () => {
        const readModel = slice_named(document, 'CustomerList').readModel!;
        [readModel.name, Object.keys(readModel.schema.properties as object)].should.deep.equal(['CustomerSummary', ['customerId', 'name', 'visits']]);
    });

    it('should point a consumed event at the event its producer declares', () => {
        const produced = slice_named(document, 'Register').events.find(event => event.name === 'CustomerRegistered')!;
        const consumed = slice_named(document, 'CustomerList').events.find(event => event.name === 'CustomerRegistered')!;
        [consumed.sourceEventId, consumed.schema].should.deep.equal([produced.id, produced.schema]);
    });

    it('should carry the queries with their parameters', () => {
        slice_named(document, 'CustomerList').queries.map(query => [query.name, query.parameters.map(parameter => parameter.name)]).should.deep.equal([
            ['CustomerById', ['customerId']],
            ['AllCustomers', ['name']],
        ]);
    });

    it('should show what sets an automation off', () => {
        const trigger = slice_named(document, 'Reminders').automationTrigger!;
        [trigger.description, trigger.eventId].should.deep.equal(['Right after registering', slice_named(document, 'Register').events[0].id]);
    });

    it('should carry the specifications with their literal values', () => {
        const specification = slice_named(document, 'Register').specifications[0];
        [specification.when!.values, specification.when!.commandId].should.deep.equal([
            { customerId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', name: 'Ada', age: 36 },
            slice_named(document, 'Register').command!.id,
        ]);
    });

    it('should carry only the named errors a specification expects', () => {
        slice_named(document, 'Register').specifications[1].thenErrors.map(error => error.name).should.deep.equal(['Too young']);
    });
});
