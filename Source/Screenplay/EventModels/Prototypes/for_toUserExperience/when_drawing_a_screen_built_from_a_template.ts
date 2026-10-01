// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { PrototypeElementDocument } from '../../Document/EventModelDocument';
import { toUserExperience } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing a screen built from a template', () => {
    let elements: PrototypeElementDocument[];

    beforeEach(() => {
        elements = toUserExperience(screens_of(
            'screen Details',
            '  data Invoice via query InvoiceById by id',
            '  template Dashboard',
            '    header',
            '      title "Invoice"',
            '    sidebar',
            '      summary invoice',
            '        field number label "Number"',
            '    main',
            '      section lines',
            '        ```html',
            '<p/>',
            '        ```',
            '      navigate to Invoices',
            '    empty',
            '    footer',
        ), 'M#S')[0].elements;
    });

    it('should span the header and lay the other slots out side by side, leaving the data to the summary', () =>
        elements.map(drawn).should.deep.equal([
            ['label', 'Invoice', 24, 24, 852, 32],
            ['panel', 'invoice', 24, 72, 273.3333333333333, 56],
            ['content-area', 'html', 313.3333333333333, 72, 273.3333333333333, 140],
            ['button-hot', 'Invoices', 313.3333333333333, 228, 160, 40],
        ]));
});
