// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { SliceReferenceCollector } from '../SliceReferences';
import { SyntaxNode } from '../../Syntax/SyntaxNode';

describe('when a trigger value leaves its type unstated', () => {
    let collector: SliceReferenceCollector;
    beforeEach(() => {
        collector = new SliceReferenceCollector();
        collector.visitTriggerData({ kind: 'TriggerDataSyntax', name: 'id', type: null, location: { line: 1, column: 1 } });
        collector.visitNode({ kind: 'FutureSyntax', location: { line: 2, column: 1 } } satisfies SyntaxNode);
    });
    it('should not infer a shared type or dependency', () => {
        collector.shared.should.have.lengthOf(0);
        collector.references.should.have.lengthOf(0);
    });
});
