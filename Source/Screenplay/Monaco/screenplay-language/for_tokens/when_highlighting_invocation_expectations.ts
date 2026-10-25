// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { createTokensProvider } from '../tokens';

const rules = createTokensProvider([]).tokenizer.root as unknown as [RegExp, unknown][];
const invocation = rules.find(rule => rule[0].test('        then invokes CloseInvoice'));

describe('when highlighting invocation expectations', () => {
    it('should read then invokes as a keyword before the command name', () => {
        (invocation![1] as unknown[]).should.deep.equal(['white', 'keyword', 'white', 'type.identifier']);
    });
});
