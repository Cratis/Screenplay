// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { invalidActionPresentation } from '../ScreenActionPresentation';
import { PresentationValueSyntax } from '../Screens';

// Malformed typed values can reach the shared validator independently of the JSON reader's
// schema checks. They must produce an authoring reason rather than throw during inspection.
describe('when validating advisory values', () => {
    for (const value of [null, { name: null, value: 'row-menu' }, { name: ' ', value: 'row-menu' }, { name: 'placement', value: null }]) {
        it(`should report a malformed typed value: ${JSON.stringify(value)}`, () => {
            expect(invalidActionPresentation([value] as unknown as PresentationValueSyntax[], 'Item')).toBe('Action presentation requires a key and string value.');
        });
    }
});
