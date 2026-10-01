// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AutoMapMode, MappingSyntax, ProjectionBlockSyntax, ProjectionSyntax } from '@cratis/screenplay-compiler';
import { JsonSchemaObject } from '../Document/EventModelDocument';

type Properties = Record<string, JsonSchemaObject>;

// The shape of a read model nothing declares, as the projection building it implies: automap copies every
// property an event shares with it - on unless something turns it off - each mapping adds the property it
// sets, and children and nested objects add theirs below. A mapping only says which property it sets, not
// from what, so a set property takes the type of the event property of the same name, and text otherwise.
export function readModelSchemaFromProjection(projection: ProjectionSyntax, schemaOf: (event: string) => JsonSchemaObject): JsonSchemaObject {
    const properties: Properties = {};
    addBlocks(projection.blocks, enabled(projection.autoMap, true), properties, schemaOf);
    return { type: 'object', properties };
}

function enabled(mode: AutoMapMode, inherited: boolean): boolean {
    return mode === 'Inherit' ? inherited : mode === 'Enabled';
}

function addBlocks(blocks: readonly ProjectionBlockSyntax[], autoMap: boolean, properties: Properties, schemaOf: (event: string) => JsonSchemaObject): void {
    for (const block of blocks) {
        switch (block.kind) {
            case 'FromSyntax': {
                const events = block.events.map(spec => propertiesOf(schemaOf(spec.event)));
                addEvents(events, autoMap, properties);
                addMappings(block.mappings, events, properties);
                break;
            }
            case 'EverySyntax':
            case 'AllSyntax':
                addMappings(block.mappings, [], properties);
                break;
            case 'JoinSyntax':
                for (const joined of block.events) {
                    const events = [propertiesOf(schemaOf(joined.event))];
                    addEvents(events, enabled(joined.autoMap, autoMap), properties);
                    addMappings(joined.mappings, events, properties);
                }
                break;
            case 'ChildrenSyntax': {
                const items: Properties = {};
                addBlocks(block.blocks, enabled(block.autoMap, autoMap), items, schemaOf);
                properties[block.property] = { type: 'array', items: { type: 'object', properties: items } };
                break;
            }
            case 'NestedSyntax': {
                const nested: Properties = {};
                addBlocks(block.blocks, enabled(block.autoMap, autoMap), nested, schemaOf);
                properties[block.property] = { type: 'object', properties: nested };
                break;
            }
            case 'ProjectionVariantSyntax':
                addBlocks(block.blocks, autoMap, properties, schemaOf);
                break;
        }
    }
}

function propertiesOf(schema: JsonSchemaObject): Properties {
    return (schema.properties as Properties | undefined) ?? {};
}

function addEvents(events: readonly Properties[], autoMap: boolean, properties: Properties): void {
    if (!autoMap) {
        return;
    }
    for (const [name, schema] of events.flatMap(event => Object.entries(event))) {
        properties[name] ??= schema;
    }
}

function addMappings(mappings: readonly MappingSyntax[], events: readonly Properties[], properties: Properties): void {
    for (const mapping of mappings) {
        if (mapping.property.startsWith('$') || mapping.kind === 'ClearMappingSyntax' && properties[mapping.property] !== undefined) {
            continue;
        }
        properties[mapping.property] = typeOf(mapping, events) ?? properties[mapping.property] ?? { type: 'string' };
    }
}

function typeOf(mapping: MappingSyntax, events: readonly Properties[]): JsonSchemaObject | undefined {
    switch (mapping.kind) {
        case 'CountMappingSyntax':
        case 'IncrementMappingSyntax':
        case 'DecrementMappingSyntax':
            return { type: 'integer' };
        case 'AddMappingSyntax':
        case 'SubtractMappingSyntax':
            return { type: 'number' };
        default:
            return events.map(event => event[mapping.property]).find(schema => schema !== undefined);
    }
}
