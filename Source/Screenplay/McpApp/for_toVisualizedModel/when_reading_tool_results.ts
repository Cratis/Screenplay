// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toVisualizedModel } from '../toVisualizedModel';

describe('when reading tool results', () => {
    it('should read the model the server sent', () => {
        const model = { application: 'Shop', documents: [{ path: 'a.play', source: 'a' }] };
        toVisualizedModel({ structuredContent: model }).should.equal(model);
    });
    it('should report an error result in the server\'s words', () => {
        const result = toVisualizedModel({ isError: true, content: [{ type: 'text', text: 'UnknownProposal' }] });
        (result as Error).message.should.equal('UnknownProposal');
    });
    it('should report a result without documents', () => (toVisualizedModel({ structuredContent: { application: 'Shop' } }) instanceof Error).should.be.true);
});
