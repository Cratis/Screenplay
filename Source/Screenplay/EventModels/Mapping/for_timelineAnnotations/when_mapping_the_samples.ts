// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { timelineOrderFindings } from '@cratis/screenplay-compiler';
import { FeatureDocument, SliceDocument } from '../../Document/EventModelDocument';
import { compileEventModelApplication } from '../compileEventModelApplication';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { SliceScope } from '../SliceScope';
import { timelineAnnotations } from '../timelineAnnotations';
import { withSliceAnnotations } from '../withSliceAnnotations';

function filesIn(folder: string): string[] {
    return readdirSync(folder, { withFileTypes: true }).flatMap(entry => {
        const path = join(folder, entry.name);
        return entry.isDirectory() ? filesIn(path) : path.endsWith('.play') ? [path] : [];
    });
}

function slicesIn(feature: FeatureDocument): SliceDocument[] {
    return [...feature.slices, ...feature.subFeatures.flatMap(slicesIn)];
}

for (const name of ['Commerce', 'TimeTracking']) {
    describe(`when mapping timeline findings in ${name}`, () => {
        const folder = resolve(__dirname, '../../../../../Samples', name);
        const compilation = compileEventModelApplication(filesIn(folder).map(file => ({ path: relative(folder, file), source: readFileSync(file, 'utf8') })));
        const reported = compilation.diagnostics.filter(diagnostic => ['PLAY0516', 'PLAY0517'].includes(diagnostic.code));
        let slices: SliceDocument[];

        beforeEach(() => {
            const document = withSliceAnnotations(toEventModelDocument(compilation.value, name), timelineAnnotations(compilation.value, compilation.diagnostics));
            slices = document.collections.flatMap(collection => collection.modules).flatMap(module => module.features).flatMap(slicesIn);
        });

        it('should mark every reported finding exactly once', () => {
            reported.should.not.be.empty;
            const annotations = slices.flatMap(slice => slice.annotations ?? []).filter(annotation => annotation.code !== undefined);
            annotations.should.have.lengthOf(reported.length);
            for (const diagnostic of reported) {
                annotations.filter(annotation => annotation.code === diagnostic.code && annotation.message === diagnostic.message).should.have.lengthOf(1);
            }
        });

        it('should place every finding on its consumer slice', () => {
            for (const finding of timelineOrderFindings(compilation.value)) {
                const scope = finding.consumerScope;
                const container = scope.slice(1, -1).reduce((outer, name) => outer.feature(name), SliceScope.module(scope[0]));
                const slice = slices.find(slice => slice.id === container.slice(scope[scope.length - 1]).id)!;
                slice.annotations!.should.deep.include({ severity: 'information', code: finding.diagnostic.code, message: finding.diagnostic.message });
            }
        });
    });
}
