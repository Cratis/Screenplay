// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const prefix = 'module Projects\n  feature Naming\n    slice StateChange Rename\n';
const command = '      command Rename\n        projectId Uuid identifier\n        name String\n';

describe('when mapping inline events', () => {
    it('should draw the same event identity and shape as an extracted declaration', () => {
        const inline = toEventModelDocument(parse(prefix + command + '        produces event Renamed\n          name String = name\n').value, 'Projects');
        const explicit = toEventModelDocument(parse(prefix + command + '        produces Renamed\n          for projectId\n          name = name\n      event Renamed\n        name String\n').value, 'Projects');
        expect(slice_named(inline, 'Rename').events).toEqual(slice_named(explicit, 'Rename').events);
        expect(slice_named(inline, 'Rename').events[0].name).toBe('Renamed');
    });
});
