// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver, ApplicationSyntax, ApplicationSyntaxVisitor, FeatureSyntax, ModuleSyntax } from '@cratis/screenplay-compiler';
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
        const owners = new EventOwners(syntax.modules, new SchemaSynthesizer(syntax), new AuthoringProductionResolver(syntax));
        const audience = Audience.of(syntax.personas);
        const modules = syntax.modules.map((module, index) => toModule(module, index, owners, audience));
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

// The system acts in automations and translations; without one there is no row for it.
function hasSystemSlices(modules: readonly ModuleSyntax[]): boolean {
    const inFeature = (feature: FeatureSyntax): boolean =>
        feature.slices.some(slice => slice.type === 'Automation' || slice.type === 'Translate') || feature.features.some(inFeature);
    return modules.some(module => module.features.some(inFeature));
}

function toModule(module: ModuleSyntax, sortOrder: number, owners: EventOwners, audience: Audience): ModuleDocument {
    const scope = SliceScope.module(module.name);
    const within = audience.within(module.authorize);
    return {
        id: scope.id,
        name: module.name,
        features: module.features.map(feature => toFeature(feature, scope.feature(feature.name), owners, within)),
        collapsed: false,
        sortOrder,
        commentCount: 0,
    };
}

function toFeature(feature: FeatureSyntax, scope: SliceScope, owners: EventOwners, audience: Audience): FeatureDocument {
    const within = audience.within(feature.authorize);
    return {
        id: scope.id,
        name: feature.name,
        subFeatures: feature.features.map(child => toFeature(child, scope.feature(child.name), owners, within)),
        slices: feature.slices.map((slice, index) => toSlice(slice, scope.slice(slice.name), index, owners, within.actorsFor(slice))),
        collapsed: false,
        rowCollapsed: false,
        enabled: true,
        commentCount: 0,
    };
}
