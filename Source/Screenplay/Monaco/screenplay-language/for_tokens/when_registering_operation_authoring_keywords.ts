// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { constructKeywords } from '../language';
import { createTokensProvider } from '../tokens';

const provider = createTokensProvider([]);

describe('when registering operation authoring keywords', () => {
    it.each(['operation', 'system'])('should register %s without globally reserving ordinary names', word => {
        expect(constructKeywords).toContain(word);
        expect(provider.keywords).not.toContain(word);
    });
    it.each(['operation', 'system', 'uses', 'execute', 'compensate'])('should retain keyword-named command fields and deeper legacy property indentation for %s', word => {
        const result = parse(`module M\n  feature F\n    slice StateChange S\n      command C\n        ${word} String\n          deeper String\n`);
        expect(result.success).toBe(true);
        expect(result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual([word, 'deeper']);
    });
});
