// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { defaultViewOptions, ViewOptionsStore } from '../ViewOptionsStore';
import { a_memento } from './given/a_memento';

const key = 'screenplay.eventModelBoard.viewOptions';

describe('when keeping the view options', () => {
    it('should start from the default view before anything is chosen', () =>
        new ViewOptionsStore(a_memento()).options.should.deep.equal(defaultViewOptions));

    it('should give back what was chosen', async () => {
        const store = new ViewOptionsStore(a_memento());
        await store.save({ detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' });
        store.options.should.deep.equal({ detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' });
    });

    it('should keep what was chosen for a later session', async () => {
        const storage = a_memento();
        await new ViewOptionsStore(storage).save({ detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' });
        new ViewOptionsStore(storage).options.detailLevel.should.equal('overview');
    });

    it('should fall back to the default for each option it cannot read, keeping the others', () =>
        new ViewOptionsStore(a_memento({ [key]: { detailLevel: 'everything', showProperties: 'yes', visualizationMode: 'fillLines' } })).options
            .should.deep.equal({ ...defaultViewOptions, visualizationMode: 'fillLines' }));
});
