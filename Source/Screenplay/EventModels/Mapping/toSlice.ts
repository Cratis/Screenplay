// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConstraintSyntax, QueryParameterSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import {
    EventItemDocument, QueryItemDocument, QueryParameterType, ReadModelItemDocument, SliceDocument, SliceStatus, SliceType,
} from '../Document/EventModelDocument';
import { consumedEvents } from './consumedEvents';
import { EventOwners } from './EventOwners';
import { SliceScope } from './SliceScope';
import { toAutomationTrigger } from './toAutomationTrigger';
import { toCommand } from './toCommand';
import { toSpecifications } from './toSpecifications';

const sliceTypes: Record<SliceSyntax['type'], number> = {
    StateChange: SliceType.stateChange,
    StateView: SliceType.stateView,
    Automation: SliceType.automation,
    Translate: SliceType.translator,
};

// One slice as the board draws it - the port of Studio's SliceSyntaxVisitor. A State View has no command
// and shows its read model; every slice that is not a State Change also shows the events it consumes,
// pointing back at the slice that produces each.
export function toSlice(slice: SliceSyntax, scope: SliceScope, sortOrder: number, owners: EventOwners): SliceDocument {
    const sliceType = sliceTypes[slice.type];
    const isStateView = slice.type === 'StateView';
    const command = isStateView || slice.commands.length === 0 ? undefined : toCommand(slice.commands[0], scope, owners.schemas);
    const document: SliceDocument = {
        id: scope.id,
        name: slice.name,
        sliceType,
        description: slice.description ?? '',
        status: SliceStatus.notStarted,
        collapsed: false,
        sortOrder,
        externalEvents: [],
        events: eventsOf(slice, scope, owners),
        queries: slice.queries.map(query => ({
            id: scope.idOf('query', query.name),
            name: query.name,
            parameters: [query.by, ...query.filters].filter(parameter => parameter !== null).map(parameter => parameterOf(parameter, owners)),
        }) satisfies QueryItemDocument),
        actors: [],
        specifications: toSpecifications(slice.specifications, scope, owners, command?.id),
        commentCount: 0,
    };
    if (command !== undefined) {
        document.command = command;
    }
    const readModel = isStateView ? readModelOf(slice, scope, owners) : undefined;
    if (readModel !== undefined) {
        document.readModel = readModel;
    }
    const trigger = slice.type === 'Automation' || slice.type === 'Translate' ? toAutomationTrigger(slice.reactions, owners) : undefined;
    if (trigger !== undefined) {
        document.automationTrigger = trigger;
    }
    return document;
}

function eventsOf(slice: SliceSyntax, scope: SliceScope, owners: EventOwners): EventItemDocument[] {
    const declared = slice.events.filter(event => event.name.trim().length > 0).map(event => withConstraints({
        id: owners.idFor(event.name) ?? scope.idOf('event', event.name),
        name: event.name,
        schema: owners.schemas.forProperties(event.properties),
    }, slice.constraints));
    if (slice.type === 'StateChange') {
        return declared;
    }
    const declaredNames = new Set(declared.map(event => event.name.toLowerCase()));
    const consumed = consumedEvents(slice).filter(name => !declaredNames.has(name.toLowerCase())).map(name => {
        const producer = owners.idFor(name);
        const event: EventItemDocument = { id: scope.idOf('consumes', name), name, schema: owners.schemaFor(name) };
        return producer === undefined ? event : { ...event, sourceEventId: producer };
    });
    return [...declared, ...consumed];
}

function withConstraints(event: EventItemDocument, constraints: readonly ConstraintSyntax[]): EventItemDocument {
    for (const constraint of constraints.flatMap(rule => [rule, ...rule.additionalRules])) {
        if (constraint.kind === 'FileConstraintSyntax' || constraint.event.toLowerCase() !== event.name.toLowerCase()) {
            continue;
        }
        const described = { name: constraint.name, message: constraint.message ?? '' };
        event = constraint.kind === 'UniqueEventConstraintSyntax'
            ? { ...event, constraints: { ...event.constraints, uniqueEventType: described } }
            : { ...event, constraints: { ...event.constraints, unique: described } };
    }
    return event;
}

// The read model a State View shows: the one its projection builds, else the one its first query returns,
// else the first it declares. Its shape is the declaration of that name when the slice has one.
function readModelOf(slice: SliceSyntax, scope: SliceScope, owners: EventOwners): ReadModelItemDocument | undefined {
    const name = slice.projections.find(projection => projection.readModel !== null)?.readModel
        ?? slice.queries[0]?.returnType.name
        ?? slice.readModels[0]?.name;
    if (name === undefined || name.length === 0) {
        return undefined;
    }
    const declared = slice.readModels.find(readModel => readModel.name === name);
    return {
        id: scope.idOf('readmodel', name),
        name,
        schema: declared === undefined ? {} : owners.schemas.forProperties(declared.properties),
        materializes: true,
    };
}

function parameterOf(parameter: QueryParameterSyntax, owners: EventOwners): { name: string; type: number } {
    const schema = owners.schemas.forType(parameter.type);
    const type = schema.format === 'uuid' ? QueryParameterType.uniqueId
        : schema.format === 'date' || schema.format === 'date-time' ? QueryParameterType.date
            : schema.type === 'integer' || schema.type === 'number' ? QueryParameterType.number
                : schema.type === 'boolean' ? QueryParameterType.boolean
                    : QueryParameterType.text;
    return { name: parameter.name, type };
}
