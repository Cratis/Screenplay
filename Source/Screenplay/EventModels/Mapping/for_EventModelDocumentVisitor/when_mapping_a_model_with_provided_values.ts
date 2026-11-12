// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

describe('when mapping a model with provided values', () => {
    let document: EventModelDocument;
    let plain: EventModelDocument;
    const source = (withProvide: boolean): string => [
        ...withProvide ? ['provide region String from host'] : [],
        'module Billing',
        ...withProvide ? ['  provide currency String from $context.tenant'] : [],
        '  feature Invoicing',
        ...withProvide ? ['    provide limit Int optional', '      file Limit.cs'] : [],
        '    slice StateChange Register',
        ...withProvide ? ['      provide zone String from $context.tenant'] : [],
        '      command Register',
        '        invoiceId Guid',
        '      event InvoiceRegistered',
        '        invoiceId Guid',
    ].join('\n');

    beforeAll(() => {
        document = toEventModelDocument(parse(source(true)).value, 'Billing');
        plain = toEventModelDocument(parse(source(false)).value, 'Billing');
    });

    it('should still map the slice', () => {
        (slice_named(document, 'Register') !== undefined).should.be.true;
    });

    it('should map the slice the same as without the provided values', () => {
        JSON.stringify(slice_named(document, 'Register')).should.equal(JSON.stringify(slice_named(plain, 'Register')));
    });
});
