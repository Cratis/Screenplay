// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { splitLines } from '../SourceLineSplitter';

describe('when splitting with hash comments', () => {
    it('should strip from a hash outside a string', () => {
        splitLines('from Registered # every registration', true)[0].content.should.equal('from Registered');
    });

    it('should keep a hash inside a string', () => {
        splitLines('name = "#1"', true)[0].content.should.equal('name = "#1"');
    });

    it('should keep a hash when hash comments are off', () => {
        splitLines('name #1')[0].content.should.equal('name #1');
    });
});
