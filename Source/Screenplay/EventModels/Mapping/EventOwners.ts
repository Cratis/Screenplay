// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import { JsonSchemaObject } from '../Document/EventModelDocument';
import { SchemaSynthesizer } from '../Schemas/SchemaSynthesizer';
import { SliceScope } from './SliceScope';

interface Owned {
    readonly id: string;
    readonly schema: JsonSchemaObject;
}

// Which slice produces each event. State Change slices own the events they declare, so a slice consuming one
// elsewhere points back at it and carries its shape - as Studio's ScreenplayEventOwners does. Names compare
// without regard to case, and the first producer of a name wins.
export class EventOwners {
    readonly #owned = new Map<string, Owned>();

    constructor(modules: readonly ModuleSyntax[], readonly schemas: SchemaSynthesizer) {
        for (const module of modules) {
            for (const feature of module.features) {
                this.#register(SliceScope.module(module.name).feature(feature.name), feature);
            }
        }
    }

    idFor(name: string): string | undefined {
        return this.#owned.get(name.toLowerCase())?.id;
    }

    schemaFor(name: string): JsonSchemaObject {
        return this.#owned.get(name.toLowerCase())?.schema ?? {};
    }

    #register(scope: SliceScope, feature: FeatureSyntax): void {
        feature.slices.filter(slice => slice.type === 'StateChange').forEach(slice => this.#own(scope.slice(slice.name), slice));
        feature.features.forEach(child => this.#register(scope.feature(child.name), child));
    }

    #own(scope: SliceScope, slice: SliceSyntax): void {
        slice.events.filter(event => event.name.trim().length > 0 && !this.#owned.has(event.name.toLowerCase()))
            .forEach((event: EventSyntax) => this.#owned.set(event.name.toLowerCase(), {
                id: scope.idOf('event', event.name),
                schema: this.schemas.forProperties(event.properties),
            }));
    }
}
