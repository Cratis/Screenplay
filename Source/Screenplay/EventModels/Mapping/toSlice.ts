// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { eventDeclarations, CommandSyntax, QueryParameterSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import {
    CommandItemDocument, EventItemDocument, QueryItemDocument, QueryParameterType, ReadModelItemDocument, SliceDocument, SliceStatus, SliceType,
} from '../Document/EventModelDocument';
import { toUserExperience } from '../Prototypes/toUserExperience';
import { consumedEvents } from './consumedEvents';
import { EventConstraint } from './EventConstraint';
import { EventOwners } from './EventOwners';
import { producedEvents } from './producedEvents';
import { readModelSchemaFromProjection } from './readModelSchemaFromProjection';
import { SliceScope } from './SliceScope';
import { toAutomationTrigger } from './toAutomationTrigger';
import { toCaptureSource, toCaptureTrigger } from './toCaptureSource';
import { toCommand } from './toCommand';
import { toSpecifications } from './toSpecifications';

const sliceTypes: Record<SliceSyntax['type'], number> = {
    StateChange: SliceType.stateChange,
    StateView: SliceType.stateView,
    Automation: SliceType.automation,
    Translate: SliceType.translator,
};

// One slice as the board draws it - the port of Studio's SliceSyntaxVisitor. A State View has no command
// and shows its read model; every slice that is not a State Change also shows the events it produces through
// its reactions and captures, and the events it consumes, pointing back at the slice that produces each. An
// automation shows the command it invokes, and a translation what its captures read.
export function toSlice(slice: SliceSyntax, scope: SliceScope, sortOrder: number, owners: EventOwners, audience: readonly string[]): SliceDocument {
    const sliceType = sliceTypes[slice.type];
    const isStateView = slice.type === 'StateView';
    const command = isStateView ? undefined : commandOf(slice, scope, owners);
    const document: SliceDocument = {
        id: scope.id,
        name: slice.name,
        sliceType,
        description: slice.description ?? '',
        status: SliceStatus.notStarted,
        collapsed: false,
        sortOrder,
        externalEvents: slice.captures.map(capture => toCaptureSource(capture, scope)),
        events: eventsOf(slice, scope, owners),
        queries: slice.queries.map(query => ({
            id: scope.idOf('query', query.name),
            name: query.name,
            parameters: [query.by, ...query.filters].filter(parameter => parameter !== null).map(parameter => parameterOf(parameter, owners)),
        }) satisfies QueryItemDocument),
        actors: toUserExperience(slice.screens, scope.path, audience),
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
    const trigger = slice.type === 'Automation' || slice.type === 'Translate'
        ? toAutomationTrigger(slice.reactions, owners) ?? (slice.captures.length > 0 ? toCaptureTrigger(slice.captures[0]) : undefined)
        : undefined;
    if (trigger !== undefined) {
        document.automationTrigger = trigger;
    }
    return document;
}

function eventsOf(slice: SliceSyntax, scope: SliceScope, owners: EventOwners): EventItemDocument[] {
    const constraintsByEvent = new Map<string, EventConstraint[]>();
    for (const constraint of slice.constraints.flatMap(rule => [rule, ...rule.additionalRules])) {
        if (constraint.kind === 'FileConstraintSyntax') continue;
        const name = constraint.event.toLowerCase();
        const rules = constraintsByEvent.get(name) ?? [];
        rules.push(constraint);
        constraintsByEvent.set(name, rules);
    }
    const declared = eventDeclarations(slice).filter(event => event.name.trim().length > 0).map(event => withConstraints({
        id: owners.idFor(event.name) ?? scope.idOf('event', event.name),
        name: event.name,
        schema: owners.schemas.forProperties(event.properties),
    }, constraintsByEvent.get(event.name.toLowerCase()) ?? []));
    if (slice.type === 'StateChange') {
        return declared;
    }
    const declaredNames = new Set(declared.map(event => event.name.toLowerCase()));
    const produced = producedEvents(slice, owners.productions).filter(name => !declaredNames.has(name.toLowerCase())).map(name => {
        declaredNames.add(name.toLowerCase());
        return { id: scope.idOf('produces', name), name, schema: owners.schemaFor(name) } satisfies EventItemDocument;
    });
    const consumed = consumedEvents(slice).filter(name => !declaredNames.has(name.toLowerCase())).map(name => {
        const producer = owners.idFor(name);
        const event: EventItemDocument = { id: scope.idOf('consumes', name), name, schema: owners.schemaFor(name) };
        return producer === undefined ? event : { ...event, sourceEventId: producer };
    });
    return [...declared, ...produced, ...consumed];
}

// The slice's own command, else - for an automation - the first command its reactions invoke, shaped by
// its declaration when the model has one.
function commandOf(slice: SliceSyntax, scope: SliceScope, owners: EventOwners): CommandItemDocument | undefined {
    if (slice.commands.length > 0) {
        return toCommand(slice.commands[0], scope, owners.schemas, owners);
    }
    const invoked = slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.invokes)[0];
    if (invoked === undefined) {
        return undefined;
    }
    const declared: CommandSyntax | undefined = owners.commandNamed(invoked.command);
    return declared === undefined
        ? { id: scope.idOf('command', invoked.command), name: invoked.command, schema: {}, stateSchema: {}, logicDescription: '', rules: [] }
        : toCommand(declared, scope, owners.schemas, owners);
}

function withConstraints(event: EventItemDocument, constraints: readonly EventConstraint[]): EventItemDocument {
    for (const constraint of constraints) {
        const described = { name: constraint.name, message: constraint.message ?? '' };
        event = constraint.kind === 'UniqueEventConstraintSyntax'
            ? { ...event, constraints: { ...event.constraints, uniqueEventType: described } }
            : { ...event, constraints: { ...event.constraints, unique: described } };
    }
    return event;
}

// The read model a State View shows: the one its projection builds, else the one its first query returns,
// else the first it declares. Its shape is the declaration of that name when the slice has one, else what
// the projection building it implies.
function readModelOf(slice: SliceSyntax, scope: SliceScope, owners: EventOwners): ReadModelItemDocument | undefined {
    const name = slice.projections.find(projection => projection.readModel !== null)?.readModel
        ?? slice.queries[0]?.returnType.name
        ?? slice.readModels[0]?.name;
    if (name === undefined || name.length === 0) {
        return undefined;
    }
    const declared = slice.readModels.find(readModel => readModel.name === name);
    const projection = slice.projections.find(candidate => candidate.readModel === name);
    return {
        id: scope.idOf('readmodel', name),
        name,
        schema: declared !== undefined ? owners.schemas.forProperties(declared.properties)
            : projection !== undefined ? readModelSchemaFromProjection(projection, event => owners.schemaFor(event)) : {},
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
