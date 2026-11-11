// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

// Abilities are declared contracts. Until an invocation names one, nothing leaves a slice, so the board draws
// exactly what it draws without them: no row for the system, and no extra element in the timeline.
const model = (systems: string[]) => [
    ...systems,
    'module Billing',
    '  feature Invoicing',
    '    slice StateChange Register',
    '      command Register',
    '        invoiceId Guid',
    '      event InvoiceRegistered',
    '        invoiceId Guid',
];

describe('when mapping systems with abilities', () => {
    let plain: EventModelDocument;
    let withAbilities: EventModelDocument;

    beforeAll(() => {
        plain = toEventModelDocument(parse(model([]).join('\n')).value, 'Billing');
        withAbilities = toEventModelDocument(parse(model([
            'system Carrier',
            '  ability BookPickup idempotent',
            '    invoiceId Guid',
            '    returns',
            '      trackingNumber String',
            '    outcome NoCapacity',
        ]).join('\n')).value, 'Billing');
    });

    it('should draw the same board as without the system', () => expect(JSON.stringify(withAbilities)).toBe(JSON.stringify(plain)));
});
