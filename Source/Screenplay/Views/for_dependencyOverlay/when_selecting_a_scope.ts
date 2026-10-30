// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { overlayState } from '../dependencyOverlay';
import { overlay } from './given/an_overlay';

describe('when selecting a scope', () => {
    const state = overlayState(overlay, 'b', new Set(['a', 'b', 'c', 'm', 'x']));
    it('should remember the selection', () => state.selectedId!.should.equal('b'));
    it('should emphasise the scope as selected', () => state.emphasis('b')!.should.equal('selected'));
    it('should emphasise what it depends on', () => state.emphasis('c')!.should.equal('dependency'));
    it('should emphasise what depends on it', () => state.emphasis('a')!.should.equal('dependant'));
    it('should dim everything else', () => state.emphasis('x')!.should.equal('dimmed'));
    it('should not dim what holds an emphasised slice', () => (state.emphasis('m') === undefined).should.be.true);
    it('should map the links', () => state.links.should.deep.equal([{ from: 'a', to: 'b', emphasis: 'within' }, { from: 'b', to: 'c', emphasis: 'within' }]));
});

describe('when selecting a scope with a crossing link', () => {
    it('should mark the link as crossing', () => overlayState(overlay, 'a', new Set(['a', 'b', 'c'])).links.find(link => link.to === 'c' && link.from === 'a')!.emphasis.should.equal('crossing'));
});
