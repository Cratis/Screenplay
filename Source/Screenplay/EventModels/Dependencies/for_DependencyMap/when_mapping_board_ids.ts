// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync } from 'node:fs';
import { describe, it } from 'vitest';
import { toEventModelDocument } from '../../Mapping/EventModelDocumentVisitor';
import type { FeatureDocument } from '../../Document/EventModelDocument';
import { DependencyGraph } from '@cratis/screenplay-compiler';
import { boardIdOf } from '../boardIdOf';
import { sampleApplication, samplesDirectory } from './given/a_sample';

describe('when mapping board identities', () => {
    const missing: string[] = [];
    for (const sample of readdirSync(samplesDirectory, { withFileTypes: true }).filter(entry => entry.isDirectory())) {
        const application = sampleApplication(sample.name);
        const ids = new Set<string>();
        const features = (items: FeatureDocument[]) => items.forEach(feature => {
            ids.add(feature.id);
            feature.slices.forEach(slice => ids.add(slice.id));
            features(feature.subFeatures);
        });
        toEventModelDocument(application, sample.name).collections.forEach(collection => collection.modules.forEach(module => { ids.add(module.id); features(module.features); }));
        missing.push(...DependencyGraph.for(application).nodes.filter(node => node.kind !== 'context' && !ids.has(boardIdOf(node)!)).map(node => `${sample.name}:${node.key}`));
    }
    it('should use existing board ids for every slice feature and module in every sample', () => missing.should.deep.equal([]));
});
