// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const prefix = 'module Projects\n  feature Naming\n    slice StateChange Rename\n';
const command = '      command Rename\n        projectId Uuid identifier\n        name String\n';

describe('when mapping inline events', () => {
    it('should keep event constraint matching linear', () => {
        const measure = (count: number) => {
            const source = prefix + command + Array.from({ length: count }, (_, index) => `        produces event Recorded${index}\n          name String = name`).join('\n') + '\n' +
                Array.from({ length: count }, (_, index) => `      constraint Unique${index}\n        unique event Recorded${index}`).join('\n');
            const application = parse(source).value;
            const lowercase = vi.spyOn(String.prototype, 'toLowerCase');
            let calls: number;
            try {
                const document = toEventModelDocument(application, 'Projects');
                calls = lowercase.mock.calls.length;
                const events = slice_named(document, 'Rename').events;
                expect(events).toHaveLength(count);
                expect(events.every((event, index) => event.constraints?.uniqueEventType?.name === `Unique${index}`)).toBe(true);
            } finally {
                lowercase.mockRestore();
            }
            return calls;
        };
        const small = measure(100);
        const large = measure(200);
        expect(small).toBeGreaterThan(0);
        expect(large).toBeLessThan(small * 2.2);
    });

    it('should keep board mappings and required fields identical for both optionality spellings', () => {
        const source = prefix + command + '        tags String[]?\n        produces event Renamed\n          note String? = name\n          tags String[]? = tags\n';
        const legacy = toEventModelDocument(parse(source).value, 'Projects');
        const canonical = toEventModelDocument(parse(source.replaceAll('?', ' optional')).value, 'Projects');
        expect(canonical).toEqual(legacy);
        expect(slice_named(canonical, 'Rename').events[0].schema.required ?? []).toEqual([]);
    });

    it('should draw the same event identity and shape as an extracted declaration', () => {
        const inline = toEventModelDocument(parse(prefix + command + '        produces event Renamed\n          name String = name\n').value, 'Projects');
        const explicit = toEventModelDocument(parse(prefix + command + '        produces Renamed\n          for projectId\n          name = name\n      event Renamed\n        name String\n').value, 'Projects');
        expect(slice_named(inline, 'Rename').events).toEqual(slice_named(explicit, 'Rename').events);
        expect(slice_named(inline, 'Rename').events[0].name).toBe('Renamed');
    });
});
