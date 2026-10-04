// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { constructKeywords } from '../language';
import { createTokensProvider } from '../tokens';

const provider = createTokensProvider([]);

describe('when registering source-stream authoring grammar', () => {
    it('should register eventsource without globally reserving existing property names', () => {
        expect(constructKeywords).toContain('eventsource');
        expect(provider.keywords).not.toContain('eventsource');
        expect(provider.keywords).not.toContain('stream');
    });
    it.each(['eventsource', 'stream', 'streamId', 'identifier', 'fromstream', '@eventsource', '@stream'])('should retain command property %s and its deeper legacy members', name => {
        const result = parse(`module M\n  feature F\n    slice StateChange S\n      command C\n        ${name} String\n          deeper String`);
        expect(result.success).toBe(true);
        expect(result.value.modules[0].features[0].slices[0].commands[0].stream).toBeNull();
        expect(result.value.modules[0].features[0].slices[0].commands[0].properties).toHaveLength(2);
    });
});
