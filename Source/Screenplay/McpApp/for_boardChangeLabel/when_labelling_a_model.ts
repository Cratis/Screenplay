// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { boardChangeLabel } from '../boardChangeLabel';

const set = (added: number, changed: number, removed: number) => ({ changes: new Map(), added, changed, removed });

describe('when labelling a model', () => {
    it('should say what is added and changed', () => boardChangeLabel('Proposed', set(3, 1, 0)).should.equal('Proposed · 3 added, 1 changed'));
    it('should say what is removed', () => boardChangeLabel('Current', set(0, 0, 2)).should.equal('Current · 2 removed'));
    it('should leave out what there is none of', () => boardChangeLabel('Proposed', set(0, 4, 0)).should.equal('Proposed · 4 changed'));
    it('should keep the name alone when nothing is marked', () => boardChangeLabel('Proposed', set(0, 0, 0)).should.equal('Proposed'));
});
