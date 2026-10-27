// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { scanDocument } from '../symbols';

describe('when declaring a concept with a keyword modifier', () => {
    const symbols = scanDocument(['concept SpeakerName : String pii secret', '  validate', '    not empty']);

    it('should know the concept', () => symbols.concepts.map(concept => concept.name).should.deep.equal(['SpeakerName']));
    it('should keep the modifiers', () => symbols.concepts[0].attributes.should.deep.equal(['pii', 'secret']));
});
