// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        recipient String optional\n        compensate\n      event Recorded\n      command C\n        produces Recorded\n        produces Send\n      specification T\n        given operation Send fails\n        when C\n        then operation Send\n          recipient = "Ada"\n        then compensated Send\n';

describe('when mapping operation intent', () => {
    it('should not fabricate operation event cards or event assertion identities', () => {
        const application = parse(source);
        expect(application.success).toBe(true);
        const document = toEventModelDocument(application.value, 'Example');
        const slice = document.collections[0].modules[0].features[0].slices[0];
        expect(slice.events.map(event => event.name)).toEqual(['Recorded']);
        expect(slice.specifications[0].given).toEqual([]);
        expect(slice.specifications[0].thenEvents).toEqual([]);
        expect(slice.description).toContain('ESM v9');
        expect(slice.description).toContain('Send');
        expect(slice.description).toContain('recipient');
        expect(slice.description).toContain('Ada');
    });
    it('should not fabricate event identities from mixed-kind ambiguous productions', () => {
        const application = parse('system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      operation Send\n        uses Mailer\n    slice Automation Observe\n      reaction R\n        every 1 day\n          produces Send\n');
        expect(application.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0497');
        const document = toEventModelDocument(application.value, 'Example');
        expect(document.collections[0].modules[0].features[0].slices[2].events).toEqual([]);
    });
    it('should exclude even invalid reaction operations before constructing produced event cards', () => {
        const application = parse(source + '    slice Automation Observe\n      reaction R\n        every 1 day\n          produces Send\n');
        expect(application.success).toBe(false);
        const document = toEventModelDocument(application.value, 'Example');
        expect(document.collections[0].modules[0].features[0].slices[1].events).toEqual([]);
    });
});
