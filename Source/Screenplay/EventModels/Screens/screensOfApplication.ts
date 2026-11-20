// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax } from '@cratis/screenplay-compiler';
import { screenDocumentsOfSlice, type ConversionFinding, type ImportedScreenDocument } from '@cratis/scene.screenplay';
import { screenDocumentKey } from './screenDocumentKey';
import { sliceScreensOfApplication, type ScreenActor } from './sliceScreens';

// The Scene documents of an application's screens, and what a screen said that could not be drawn. A host that
// can draw a screen looks a document up by slice and actor; a role with none keeps the board's placeholder.
export interface ApplicationScreens {
    readonly documents: Readonly<Record<string, string>>;
    readonly findings: readonly ConversionFinding[];
}

export const noApplicationScreens: ApplicationScreens = { documents: {}, findings: [] };

// Reads every slice's screens back as the Scene document a host draws, keyed by the identity the board's own
// document gives the slice and the actor. The conversion itself belongs to Scene (@cratis/scene.screenplay),
// which mirrors the bridge Studio's backend uses, so Screenplay never has a second opinion about what a screen
// is; all that happens here is deciding whose row each document is drawn on.
//
// A screen named after a persona is that persona's; every other screen of a slice is the slice's own and is
// drawn on every row the slice has. Nothing here ever throws: a screen that cannot be converted is reported as
// a finding and simply has no document, which leaves the board exactly as it was before.
export function screensOfApplication(application: ApplicationSyntax): ApplicationScreens {
    const inventory = sliceScreensOfApplication(application);
    if (inventory.slices.length === 0) {
        return noApplicationScreens;
    }

    const roles = new Set(inventory.roles);
    const documents: Record<string, string> = {};
    const findings: ConversionFinding[] = [];

    for (const slice of inventory.slices) {
        const converted = screenDocumentsOfSlice({ sliceId: slice.sliceId, screens: slice.screens, roles });
        findings.push(...converted.findings);
        for (const actor of slice.actors) {
            const document = documentFor(converted.documents, actor);
            if (document !== undefined) {
                documents[screenDocumentKey(slice.sliceId, actor.id)] = document;
            }
        }
    }

    return { documents, findings };
}

// The document an actor's row draws: the one written for the actor's own role when there is one, and otherwise
// the slice's own screens, which belong to everyone who uses the slice.
function documentFor(documents: readonly ImportedScreenDocument[], actor: ScreenActor): string | undefined {
    const named = documents.find(document => document.roleName !== undefined && document.roleName === actor.name);
    return (named ?? documents.find(document => document.roleName === undefined))?.document;
}
