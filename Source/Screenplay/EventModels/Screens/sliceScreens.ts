// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expandSpecificationExamples, ApplicationSyntax, FeatureSyntax, ModuleSyntax, ScreenSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import { Audience } from '../Mapping/Audience';
import { SliceScope } from '../Mapping/SliceScope';
import { withAdoptedScreens } from '../Mapping/withAdoptedScreens';

// One actor's row on the board, as the document identifies it.
export interface ScreenActor {
    readonly id: string;
    readonly name: string;
}

// The screens of one slice, under the identity the board's document gives that slice and the actors whose rows
// draw them. Only a slice that has screens is here - a slice without them has no user experience row at all.
export interface SliceScreens {
    readonly sliceId: string;
    readonly screens: readonly ScreenSyntax[];
    readonly actors: readonly ScreenActor[];
}

// Every slice's screens, and the roles the application declares as personas. A screen named after one belongs
// to that persona's row; every other screen belongs to the slice and is drawn on every row the slice has.
export interface ApplicationSliceScreens {
    readonly slices: readonly SliceScreens[];
    readonly roles: readonly string[];
}

// Where a screen is drawn, walked exactly as the document is walked: the same adopted screens, the same scopes
// and the same audience, so a slice and an actor are identified here by the very ids the document carries. A
// walk of its own would be a second answer to the question the document already answered.
export function sliceScreensOfApplication(application: ApplicationSyntax): ApplicationSliceScreens {
    const audience = Audience.of(application.personas, application.availabilities ?? []);
    const effective = withAdoptedScreens(expandSpecificationExamples(application));
    const names = audience.namesByActorId();
    const slices: SliceScreens[] = [];

    const inFeature = (feature: FeatureSyntax, scope: SliceScope, within: Audience): void => {
        const here = within.within(feature.authorize, feature.available);
        feature.features.forEach(child => inFeature(child, scope.feature(child.name), here));
        feature.slices.forEach(slice => add(slice, scope.slice(slice.name), here));
    };
    const inModule = (module: ModuleSyntax): void => {
        const scope = SliceScope.module(module.name);
        const within = audience.within(module.authorize, module.available);
        module.features.forEach(feature => inFeature(feature, scope.feature(feature.name), within));
    };
    effective.modules.forEach(inModule);

    return { slices, roles: application.personas.filter(persona => persona.name.length > 0).map(persona => persona.name) };

    function add(slice: SliceSyntax, scope: SliceScope, within: Audience): void {
        const actorIds = within.actorsFor(slice);
        if (slice.screens.length === 0) return;
        slices.push({
            sliceId: scope.id,
            screens: slice.screens,
            actors: actorIds.map(id => ({ id, name: names.get(id) ?? '' })),
        });
    }
}
