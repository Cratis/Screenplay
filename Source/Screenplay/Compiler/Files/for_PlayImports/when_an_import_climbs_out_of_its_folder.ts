// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when an import climbs out of its folder', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('Ordering/Ordering.play', 'module Ordering\n  import "../Shared/*.play"');
        folder.documents.set('Shared/Concepts.play', 'concept OrderId : Uuid');
        folder.documents.set('Shared/Nested/Deep.play', 'concept Deep : Uuid');
        folder.resolve('Ordering/Ordering.play');
    });

    it('should import the matching file', () => {
        folder.placementOf('Shared/Concepts.play').should.deep.equal(['Ordering']);
    });

    it('should not match across folders with a single star', () => {
        folder.resolved.some(document => document.path === 'Shared/Nested/Deep.play').should.be.false;
    });
});
