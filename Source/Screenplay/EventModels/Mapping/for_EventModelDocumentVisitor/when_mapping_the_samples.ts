// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { compileApplication, type ApplicationSyntax, type FeatureSyntax, type SliceSyntax } from '@cratis/screenplay-compiler';
import { EventModelDocument, FeatureDocument, SliceDocument, SliceType } from '../../Document/EventModelDocument';
import { userActor } from '../../Prototypes/toUserExperience';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

// The samples at the root of the repository - each folder one application, composed by its imports.
const samples = resolve(__dirname, '../../../../../Samples');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

function slicesOf(document: EventModelDocument): SliceDocument[] {
    const slices = (features: FeatureDocument[]): SliceDocument[] => features.flatMap(feature => [...feature.slices, ...slices(feature.subFeatures)]);
    return document.collections.flatMap(collection => collection.modules).flatMap(module => slices(module.features));
}

describe('when mapping the samples', () => {
    const documents = new Map<string, EventModelDocument>();
    const applications = new Map<string, ApplicationSyntax>();
    let problems: string[];

    beforeAll(() => {
        for (const sample of readdirSync(samples).filter(entry => statSync(join(samples, entry)).isDirectory())) {
            const folder = join(samples, sample);
            const files = new Map(playFiles(folder).map(file => [relative(folder, file).split('\\').join('/'), readFileSync(file, 'utf8')]));
            const application = compileApplication(files).value!;
            applications.set(sample, application);
            documents.set(sample, toEventModelDocument(application, sample));
        }
        problems = [...documents].flatMap(([sample, document]) => problems_the_board_finds_in(document).map(problem => `${sample}: ${problem}`));
    });

    it('should find the samples', () => documents.size.should.be.greaterThan(3));
    it('should map every sample into a document the board can read', () => problems.should.deep.equal([]));

    // A screen is drawn in the row of every persona allowed to use it; the samples gate every screen so that
    // one is, and none falls to the generic user.
    it('should draw every screen for a persona', () =>
        [...documents].flatMap(([sample, document]) => slicesOf(document)
            .filter(slice => slice.actors.some(actor => actor.id === userActor.id))
            .map(slice => `${sample}: ${slice.name}`)).should.deep.equal([]));

    // A state change needs a UI issuer (an action on a screen its persona already works in), held by the input-surface
    // completeness check (PLAY0533), not a screen of its own. A state view something watches - one with an observable
    // query - keeps its screen; a view only protected reads use is state, not a screen. Mirrors the C# sample harness.
    it('should give every watched state view slice a screen', () => {
        const slices = (features: readonly FeatureSyntax[]): SliceSyntax[] => features.flatMap(feature => [...feature.slices, ...slices(feature.features)]);
        return [...applications].flatMap(([sample, application]) => application.modules.flatMap(module => slices(module.features))
            .filter(slice => slice.type === 'StateView' && slice.queries.some(query => query.isObservable) && slice.screens.length === 0)
            .map(slice => `${sample}: ${slice.name}`)).should.deep.equal([]);
    });

    // Only a read of state no event builds may be specified without an action - 'then query' runs the query itself.
    it('should show what sets off every specification outside a state view', () =>
        [...documents].flatMap(([sample, document]) => slicesOf(document)
            .filter(slice => slice.sliceType !== SliceType.stateView)
            .flatMap(slice => slice.specifications.filter(specification => specification.when === undefined && specification.whenEvent === undefined).map(specification => `${sample}: ${slice.name}.${specification.name}`)))
            .should.deep.equal([]));
});
