// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';
import { SyntaxJsonValue, toSyntaxJson } from '../../Syntax/SyntaxJson';

describe('when serializing a specification document without examples', () => {
    let wire: Record<string, SyntaxJsonValue>[];

    beforeEach(() => {
        wire = parseSpecificationSource('specification First\nspecification Second', 'wire.play').value.map(root => toSyntaxJson(root) as Record<string, SyntaxJsonValue>);
    });

    it('should preserve root kinds, names and order', () => wire.map(({ kind, name }) => ({ kind, name })).should.deep.equal([
        { kind: 'SpecificationSyntax', name: 'First' }, { kind: 'SpecificationSyntax', name: 'Second' },
    ]));
    it('should omit legacy source options from every wire root', () => wire.forEach(root => Object.hasOwn(root, 'sourceOptions').should.equal(false)));
    it('should leave examples absent rather than empty or null', () => wire.forEach(root => Object.hasOwn(root, 'examples').should.equal(false)));
    it('should omit physical locations from every wire root', () => wire.forEach(root => Object.hasOwn(root, 'location').should.equal(false)));
});
