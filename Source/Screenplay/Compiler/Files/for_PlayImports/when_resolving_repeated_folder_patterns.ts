// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when resolving repeated folder patterns', () => {
    it('should resolve matches and report one unmatched pattern', () => {
        const folder = new a_folder();
        const glob = '**/**/';
        folder.documents.set('root.play', `module Orders\n  import "${glob}Found.play"\n  import "${glob}Missing.play"`);
        const path = 'a/b/Found.play';
        folder.documents.set(path, 'feature Found');
        folder.resolve('root.play');
        expect(folder.placementOf(path)).toEqual(['Orders']);
        expect(folder.diagnostics).toHaveLength(1);
        expect(folder.diagnostics[0]?.severity).toBe('warning');
    });
});
