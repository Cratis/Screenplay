// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import vectors from '../event-context-resolution-vectors.json';
import { resolveEventContextPath } from '../event-context';

describe('when resolving paths generated from the C# event-context catalog', () => {
    it('agrees with C# on every known, missing and invalid path', () => {
        expect(vectors.length).toBeGreaterThan(50);
        for (const vector of vectors) {
            expect(resolveEventContextPath(vector.path).status, vector.path).toBe(vector.status);
        }
    });
});
