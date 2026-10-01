// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { UserExperienceActorDocument } from '../../Document/EventModelDocument';
import { toUserExperience } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing screens without a presentation of their data', () => {
    let actor: UserExperienceActorDocument;

    beforeEach(() => {
        actor = toUserExperience(screens_of(
            'screen List',
            '  data Invoice[] via query AllInvoices',
            'screen One',
            '  data Invoice via query InvoiceById by id',
            '  action A',
            '  action B',
            '  action C',
            '  action D',
            '  action E',
            '  action F',
            'screen Report',
            '  file Screens/Report.tsx',
        ), 'M#S')[0];
    });

    it('should draw a collection as a table, a single item as a panel, wrap the actions and draw a file as a content area', () =>
        actor.elements.map(drawn).should.deep.equal([
            ['data-table', 'Invoice', 24, 24, 852, 220],
            ['panel', 'Invoice', 24, 260, 852, 120],
            ['button-hot', 'A', 24, 396, 160, 40],
            ['button', 'B', 200, 396, 160, 40],
            ['button', 'C', 376, 396, 160, 40],
            ['button', 'D', 552, 396, 160, 40],
            ['button', 'E', 24, 452, 160, 40],
            ['button', 'F', 200, 452, 160, 40],
            ['content-area', 'Report', 24, 508, 852, 220],
        ]));
    it('should grow the window to hold them', () => actor.windowHeight.should.equal(752));
});

describe('when drawing a slice without screens', () => {
    it('should draw nothing', () => toUserExperience([], 'M#S').should.deep.equal([]));
});
