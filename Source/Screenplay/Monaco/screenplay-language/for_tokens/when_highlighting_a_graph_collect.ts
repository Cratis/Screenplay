// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { keywordDocs } from '../keyword-docs';
import { clauseKeywords } from '../language';
import { createTokensProvider } from '../tokens';

const provider = createTokensProvider([]);

describe('when highlighting a graph collect', () => {
    it('should register collect as a clause keyword', () => expect(clauseKeywords).toContain('collect'));
    it('should highlight collect', () => expect(provider.keywords).toContain('collect'));
    it('should document collect for hover', () => expect(keywordDocs.collect).toContain('ESM v54'));
});
