// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = 'eventsource A\n  identifier String\n  stream S\n    streamId\n      projectId String\n      period String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        projectId String identifier\n        month String\n        stream A.S\n          streamId\n            period = month\n            projectId = projectId\n        produces E\n          for projectId\n      event E\n      specification Routed\n        when C\n          projectId = "p"\n          month = "2026-10"\n        then E\n          stream A.S\n            streamId\n              period = "2026-10"\n              projectId = "p"';

describe('when mapping composite stream routes', () => {
    it('should display command mappings in authored order without encoding', () => {
        expect(parse(source).diagnostics).toEqual([]);
        const slice = toEventModelDocument(parse(source).value, 'Test').collections[0].modules[0].features[0].slices[0];
        expect(slice.command?.logicDescription).toContain('Stream id: period = month, projectId = projectId');
        expect(slice.command?.logicDescription).toContain('Admitted by ESM v8');
        expect(slice.command?.logicDescription).not.toContain('PLAY0268');
        expect(slice.command?.logicDescription).not.toContain('2026-10|p');
        expect(slice.command?.logicDescription).not.toContain('p|2026-10');
    });
    it('should display each literal specification part in authored order', () => {
        const slice = toEventModelDocument(parse(source).value, 'Test').collections[0].modules[0].features[0].slices[0];
        expect(slice.specifications[0].thenEvents[0].name).toContain('streamId period = "2026-10", projectId = "p"');
        expect(slice.specifications[0].name).toContain('streamId period = "2026-10", projectId = "p"');
        expect(slice.specifications[0].name).toContain('Admitted by ESM v8');
        expect(slice.specifications[0].name).not.toContain('PLAY0268');
    });
});
