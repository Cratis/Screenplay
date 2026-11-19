// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expandSpecificationExamples, ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import { screenDocumentsOfSlice, type ConversionFinding } from '@cratis/scene.screenplay';
import { Audience } from '../Mapping/Audience';
import { SliceScope } from '../Mapping/SliceScope';
import { withAdoptedScreens } from '../Mapping/withAdoptedScreens';
import { noScreenDocuments, screenDocumentKey, type ScreenDocuments } from './screenDocumentKey';

export interface ApplicationScreens {
    // The Scene document of every screen the application states, under the key the board draws it by.
    readonly documents: ScreenDocuments;

    // What a screen said that could not be drawn, so a host can say why a screen looks thinner than the .play
    // reads. A finding is never an error: the board draws its own placeholder window for that role instead.
    readonly findings: readonly ConversionFinding[];
}

export const noApplicationScreens: ApplicationScreens = { documents: noScreenDocuments, findings: [] };

// The screens of an application as the Scene documents a host draws, keyed the way the board asks for them.
//
// The board itself holds nothing of a screen but the fact that a role has one (`hasScreen`), so the screens
// travel beside the document rather than in it. The walk is the document's own walk - the same adopted screens,
// the same slice scopes, the same audience - so every key here is a key `toEventModelDocument` wrote, and a
// screen can never land on a cell it does not belong to.
//
// A `.play` document states a module's `form` opaquely, so the forms a screen's actions would be drawn as are
// not in the tree a host hands in; such an action is drawn as its button instead. Everything a screen said that
// could not travel at all is a finding, and that role keeps the board's placeholder window.
export function screensOfApplication(application: ApplicationSyntax): ApplicationScreens {
    const effective = withAdoptedScreens(expandSpecificationExamples(application));
    if (!effective.modules.some(hasScreenInModule)) {
        return noApplicationScreens;
    }

    // The personas are declared on the application, and a Screenplay screen is never named after one: a screen
    // belongs to its slice and is drawn in the row of every actor the slice's audience admits. An empty set of
    // roles is what says so - every screen is then a screen of its own, exactly as Studio reads a screen that
    // no role claims.
    const roles = new Set<string>();
    const audience = Audience.of(application.personas, application.availabilities ?? []);
    const documents: Record<string, string> = {};
    const findings: ConversionFinding[] = [];

    const inSlice = (slice: SliceSyntax, scope: SliceScope, within: Audience): void => {
        if (slice.screens.length === 0) return;
        const sliceId = scope.id;
        const converted = screenDocumentsOfSlice({ sliceId, screens: slice.screens, roles });
        findings.push(...converted.findings);
        // Every screen of the slice is one document - the first screen is the one a thumbnail draws - and it is
        // drawn in the row of every actor the slice is for, which is exactly whose cells say they have a screen.
        const document = converted.documents[0];
        const actors = within.actorsFor(slice);
        if (document === undefined) return;
        for (const actorId of actors) {
            documents[screenDocumentKey(sliceId, actorId)] = document.document;
        }
    };

    const inFeature = (feature: FeatureSyntax, scope: SliceScope, outer: Audience): void => {
        const within = outer.within(feature.authorize, feature.available);
        feature.features.forEach(child => inFeature(child, scope.feature(child.name), within));
        feature.slices.forEach(slice => inSlice(slice, scope.slice(slice.name), within));
    };

    for (const module of effective.modules) {
        const scope = SliceScope.module(module.name);
        const within = audience.within(module.authorize, module.available);
        module.features.forEach(feature => inFeature(feature, scope.feature(feature.name), within));
    }

    return { documents, findings };
}

// Whether anything in the module has a screen at all, so an application without one converts nothing.
const hasScreenInModule = (module: ModuleSyntax): boolean => module.features.some(hasScreenInFeature);

const hasScreenInFeature = (feature: FeatureSyntax): boolean =>
    feature.slices.some(slice => slice.screens.length > 0) || feature.features.some(hasScreenInFeature);
