// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { EventVisibility, parse, TranslationDirection } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

describe('when mapping public event metadata', () => {
    it('should retain direction and opaque origins without inventing delivery behavior', () => {
        const application = parse('module M\n  feature F\n    slice Translate Publish\n      direction outbound\n      public event Published\n        number String\n      event Imported from "billing/Issued.play"\n        number String').value;
        const slice = slice_named(toEventModelDocument(application, 'Contracts'), 'Publish');
        slice.direction!.should.equal(TranslationDirection.Outbound);
        slice.events[0].visibility!.should.equal(EventVisibility.Public);
        slice.events[1].origin!.should.equal('billing/Issued.play');
        (slice.automationTrigger === undefined).should.be.true;
    });

    it('should omit metadata for legacy board documents', () => {
        const application = parse('module M\n  feature F\n    slice Translate Receive\n      event Received\n        number String').value;
        const slice = slice_named(toEventModelDocument(application, 'Contracts'), 'Receive');
        Object.hasOwn(slice, 'direction').should.be.false;
        Object.hasOwn(slice.events[0], 'visibility').should.be.false;
        Object.hasOwn(slice.events[0], 'origin').should.be.false;
    });
});
