// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DependencyGraph, parse } from '@cratis/screenplay-compiler';
import { dependencyHighlight } from '../../Dependencies/dependencyHighlight';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = `module M
  feature F
    slice StateView Lookup
      readmodel View
        id String key
        value String
    slice StateChange Change
      command Update
        id String identifier
        reads View by id
`;

const legacy = parse(source);
const protectedModel = parse(`command reads protected\n${source}`);

describe('when mapping protected command reads', () => {
    it('should parse the protected preamble without losing declarations', () => protectedModel.diagnostics.should.deep.equal([]));
    it('should keep the same command and read-model board shapes', () => {
        toEventModelDocument(protectedModel.value, 'Reads').should.deep.equal(toEventModelDocument(legacy.value, 'Reads'));
    });
    it('should retain the read dependency arrow under the preamble', () => {
        const highlight = dependencyHighlight(DependencyGraph.for(protectedModel.value), 'M.F.Change');
        highlight.dependencies.should.deep.equal(['slice:M.F.Lookup']);
        highlight.edges.should.have.lengthOf(1);
    });
});
