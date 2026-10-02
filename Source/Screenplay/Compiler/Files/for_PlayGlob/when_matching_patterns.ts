// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { hasWildcard, matchesPlayPattern, normalizePlayPath, resolvePlayPattern, staticFolderOf } from '../PlayGlob';

describe('when matching patterns', () => {
    it('should resolve relative to the importing folder', () => {
        resolvePlayPattern('Ordering/Ordering.play', 'Orders/*.play').should.equal('Ordering/Orders/*.play');
    });

    it('should resolve a pattern in a file at the root against the root', () => {
        resolvePlayPattern('application.play', './Orders/*.play').should.equal('Orders/*.play');
    });

    it('should resolve a pattern starting at the root against the root', () => {
        resolvePlayPattern('Ordering/Ordering.play', '/Shared/*.play').should.equal('Shared/*.play');
    });

    it('should climb with dot dot', () => {
        resolvePlayPattern('Ordering/Orders/Orders.play', '../../Shared/x.play').should.equal('Shared/x.play');
    });

    it('should keep climbing above the root', () => {
        normalizePlayPath('a/../../b\\c.play').should.equal('../b/c.play');
    });

    it('should match any depth with a double star', () => {
        matchesPlayPattern('Ordering/**/*.play', 'Ordering/Orders/Deep/PlaceOrder.play').should.be.true;
    });

    it('should match no folder with a double star', () => {
        matchesPlayPattern('Ordering/**/*.play', 'Ordering/Ordering.play').should.be.true;
    });

    it('should match everything below with a trailing double star', () => {
        matchesPlayPattern('Ordering/**', 'Ordering/Orders/PlaceOrder.play').should.be.true;
    });

    it('should keep a single star within a folder', () => {
        matchesPlayPattern('Ordering/*.play', 'Ordering/Orders/PlaceOrder.play').should.be.false;
    });

    it('should match one character with a question mark', () => {
        matchesPlayPattern('Slice?.play', 'Slice1.play').should.be.true;
    });

    it('should match the characters a regular expression reserves literally', () => {
        matchesPlayPattern('Orders (old)/*.play', 'Orders (old)/Place.play').should.be.true;
    });

    it('should only match play files', () => {
        matchesPlayPattern('**/*', 'Ordering/notes.md').should.be.false;
    });

    it('should take the fixed folder before the first wildcard', () => {
        staticFolderOf('Ordering/Orders/**/*.play').should.equal('Ordering/Orders');
    });

    it('should take the whole folder of a path without wildcards', () => {
        staticFolderOf('Ordering/Orders/Orders.play').should.equal('Ordering/Orders');
    });

    it('should tell a pattern from a path', () => {
        hasWildcard('Ordering/Ordering.play').should.be.false;
    });
});
