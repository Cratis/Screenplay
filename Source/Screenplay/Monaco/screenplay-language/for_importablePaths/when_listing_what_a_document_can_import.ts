// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { importablePaths } from '../file-imports';

describe('when listing what a document can import', () => {
    let paths: string[];

    beforeEach(() => {
        paths = importablePaths('Ordering/Ordering.play', [
            'application.play',
            'Ordering/Ordering.play',
            'Ordering/Orders/PlaceOrder.play',
            'Ordering/notes.md',
            'Shared/Concepts.play',
        ]);
    });

    it('should name every other play file relative to the document, with globs for the folders on the way', () => {
        paths.should.deep.equal([
            '**/*.play',
            '../**/*.play',
            '../*.play',
            '../Shared/**/*.play',
            '../Shared/*.play',
            '../Shared/Concepts.play',
            '../application.play',
            'Orders/**/*.play',
            'Orders/*.play',
            'Orders/PlaceOrder.play',
        ]);
    });
});
