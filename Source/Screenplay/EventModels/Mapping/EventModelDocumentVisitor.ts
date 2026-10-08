// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { authoredOrderKey, authoredOrderOf, expandSpecificationExamples, AuthoringProductionResolver, ApplicationSyntax, ApplicationSyntaxVisitor, FeatureSyntax, ModuleSyntax } from '@cratis/screenplay-compiler';
import { EventModelDocument, FeatureDocument, ModuleDocument } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';
import { SchemaSynthesizer } from '../Schemas/SchemaSynthesizer';
import { Audience } from './Audience';
import { EventOwners } from './EventOwners';
import { SliceScope } from './SliceScope';
import { systemActor } from './systemActor';
import { toSlice } from './toSlice';

// Where the board places the one collection a Screenplay document becomes - the origin Studio's import uses.
const collectionPosition = { x: 48, y: 48 };

// Turns an application into the document the event model board draws - the TypeScript counterpart of
// Studio's EventModelSyntaxVisitor. Screenplay has no collections, so every module goes into one; the board
// lays modules, features and slices out itself, so nothing else carries a position. Ids are derived from
// where each element sits, so compiling the same model again yields the same document.
export class EventModelDocumentVisitor implements ApplicationSyntaxVisitor<EventModelDocument> {
    constructor(private readonly name: string) {}

    visit(syntax: ApplicationSyntax): EventModelDocument {
        const audience = Audience.of(syntax.personas);
        const order = authoredOrderOf(syntax);
        // Expand against the whole declaration inventory, but retain the authored order sidecar.
        const effective = expandSpecificationExamples(syntax);
        const owners = new EventOwners(effective.modules, new SchemaSynthesizer(effective), new AuthoringProductionResolver(effective), effective.systems ?? []);
        const modules = ordered(effective.modules, [], order).map((module, index) => toModule(module, index, owners, audience, order));
        return {
            id: guidFor(`event-model:${this.name}`),
            name: this.name,
            collections: syntax.modules.length === 0 ? [] : [{
                id: guidFor(`collection:${this.name}`),
                position: collectionPosition,
                modules,
                actors: [...audience.actors(), ...(hasSystemSlices(syntax.modules) ? [systemActor] : [])],
            }],
            stickyNotes: [],
            links: [],
        };
    }
}

// Compiles nothing - it takes an application already parsed - and returns the board's document for it.
export function toEventModelDocument(application: ApplicationSyntax, name: string): EventModelDocument {
    return new EventModelDocumentVisitor(name).visit(application);
}

// Unranked declarations keep their existing order, including standalone syntax and rootless folders.
function ordered<T extends { readonly name: string }>(items: readonly T[], path: readonly string[], order: ReadonlyMap<string, number>): T[] {
    return [...items].sort((left, right) => (order.get(authoredOrderKey([...path, left.name])) ?? Number.MAX_SAFE_INTEGER)
        - (order.get(authoredOrderKey([...path, right.name])) ?? Number.MAX_SAFE_INTEGER));
}

// The system acts in automations and translations; without one there is no row for it.
function hasSystemSlices(modules: readonly ModuleSyntax[]): boolean {
    const inFeature = (feature: FeatureSyntax): boolean =>
        feature.slices.some(slice => slice.type === 'Automation' || slice.type === 'Translate') || feature.features.some(inFeature);
    return modules.some(module => module.features.some(inFeature));
}

function toModule(module: ModuleSyntax, sortOrder: number, owners: EventOwners, audience: Audience, order: ReadonlyMap<string, number>): ModuleDocument {
    const scope = SliceScope.module(module.name);
    const within = audience.within(module.authorize);
    return {
        id: scope.id,
        name: module.name,
        features: ordered(module.features, [module.name], order).map(feature => toFeature(feature, scope.feature(feature.name), owners, within, [module.name, feature.name], order)),
        collapsed: false,
        sortOrder,
        commentCount: 0,
    };
}

function toFeature(feature: FeatureSyntax, scope: SliceScope, owners: EventOwners, audience: Audience, path: readonly string[], order: ReadonlyMap<string, number>): FeatureDocument {
    const within = audience.within(feature.authorize);
    return {
        id: scope.id,
        name: feature.name,
        subFeatures: ordered(feature.features, path, order).map(child => toFeature(child, scope.feature(child.name), owners, within, [...path, child.name], order)),
        slices: ordered(feature.slices, path, order).map((slice, index) => toSlice(slice, scope.slice(slice.name), index, owners, within.actorsFor(slice))),
        collapsed: false,
        rowCollapsed: false,
        enabled: true,
        commentCount: 0,
    };
}
