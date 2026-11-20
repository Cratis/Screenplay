// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse, ApplicationSyntax } from '@cratis/screenplay-compiler';
import { EventModelDocument, SliceDocument } from '../../../Document/EventModelDocument';
import { toEventModelDocument } from '../../../Mapping/EventModelDocumentVisitor';

export const a_model = (...lines: string[]): ApplicationSyntax => parse(lines.join('\n')).value;

export const the_board_document = (application: ApplicationSyntax): EventModelDocument => toEventModelDocument(application, 'Model');

// Every slice of a board document, whatever it is nested in, so a spec can find the one it means by name.
export const slices_of = (document: EventModelDocument): SliceDocument[] =>
    document.collections.flatMap(collection => collection.modules.flatMap(module => module.features.flatMap(feature => withSubFeatures(feature))));

export const slice_named = (document: EventModelDocument, name: string): SliceDocument =>
    slices_of(document).find(slice => slice.name === name)!;

export const actor_named = (document: EventModelDocument, name: string): string =>
    document.collections[0].actors.find(actor => actor.name === name)!.id;

interface FeatureLike { readonly slices: readonly SliceDocument[]; readonly subFeatures: readonly FeatureLike[] }

const withSubFeatures = (feature: FeatureLike): SliceDocument[] => [...feature.slices, ...feature.subFeatures.flatMap(withSubFeatures)];
