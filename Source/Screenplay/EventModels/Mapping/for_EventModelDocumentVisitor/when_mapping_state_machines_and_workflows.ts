// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

// State machines and workflows are top-level declarations: they belong to the application and to no module or
// feature, so the board has no slice or lane to draw them in. They must not disturb what it does draw.
describe('when mapping state machines and workflows', () => {
    const module = [
        'module Billing',
        '  feature Invoicing',
        '    slice StateChange SendInvoice',
        '      command SendInvoice',
        '        invoiceId Guid',
        '      event InvoiceSent',
        '        invoiceId Guid',
    ];
    const lifecycles = [
        'statemachine InvoiceLifecycle',
        '  follows InvoiceView',
        '  state Draft initial',
        '    on InvoiceSent -> Sent',
        '  state Sent final',
        'workflow Chase',
        '  for InvoiceView',
        '  trigger on InvoiceSent',
        '    -> Remind',
        '  action Remind sends SendInvoice',
    ];
    let without: EventModelDocument;
    let withLifecycles: EventModelDocument;

    beforeAll(() => {
        without = toEventModelDocument(parse(module.join('\n')).value, 'Billing');
        withLifecycles = toEventModelDocument(parse([...module, ...lifecycles].join('\n')).value, 'Billing');
    });

    it('should draw the same board', () => withLifecycles.should.deep.equal(without));
    it('should map to a document the board can read', () => problems_the_board_finds_in(withLifecycles).should.deep.equal([]));
});
