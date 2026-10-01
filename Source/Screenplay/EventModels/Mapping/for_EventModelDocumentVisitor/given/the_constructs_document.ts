// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument, FeatureDocument, SliceDocument } from '../../../Document/EventModelDocument';
import { toEventModelDocument } from '../../EventModelDocumentVisitor';

// The compiler's conformance document exercises every construct the compiler models.
export const constructs_source = readFileSync(resolve(__dirname, '../../../../Compiler/Conformance/constructs.play'), 'utf8');

export const the_constructs_document = (): EventModelDocument => toEventModelDocument(parse(constructs_source).value, 'Constructs');

export function slice_named(document: EventModelDocument, name: string): SliceDocument {
    const slices = (features: FeatureDocument[]): SliceDocument[] => features.flatMap(feature => [...feature.slices, ...slices(feature.subFeatures)]);
    const found = document.collections.flatMap(collection => collection.modules).flatMap(module => slices(module.features)).find(slice => slice.name === name);
    if (found === undefined) {
        throw new Error(`The document has no slice named '${name}'`);
    }
    return found;
}
