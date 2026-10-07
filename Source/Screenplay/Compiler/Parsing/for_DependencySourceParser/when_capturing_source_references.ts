// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { captureReads, captureConcurrency, captureReducer } from '../DependencySourceParser';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';
import { splitLines } from '../SourceLineSplitter';
import { SliceReferenceCollector } from '../../Dependencies/SliceReferences';
import { SyntaxNode } from '../../Syntax/SyntaxNode';

for (const content of ['reads', 'reads R optional', 'reads R as as', 'reads R as by', 'reads R as reads']) {
    describe(`when capturing the unsupported read '${content}'`, () => {
        let captured: ReturnType<typeof captureReads>;
        beforeEach(() => { captured = captureReads(splitLines(content)[0]); });
        it('should not invent a valid read reference', () => { (captured === undefined).should.be.true; });
    });
}

describe('when capturing concurrency dimensions without committing the parser', () => {
    let context: ParserContext;
    let captured: ReturnType<typeof captureConcurrency>;
    beforeEach(() => {
        context = new ParserContext(new LineReader(splitLines('concurrency\n  eventSource\n  sourceType Orders\n  sourceType Ignored\n  streamType Public\n  streamId Standard\n  events invalid\n  events E, E\n  events Ignored\n', false, 'model.play')), 'model.play');
        captured = captureConcurrency(context, context.reader.takeSignificant());
    });
    it('should preserve the first valid dimensions and repeated event names as C# does', () => {
        captured!.should.deep.equal({ kind: 'ConcurrencySyntax', eventSource: true, eventSourceType: 'Orders', eventStreamType: 'Public', eventStreamId: 'Standard', eventTypes: ['E', 'E'], location: { path: 'model.play', line: 1, column: 1 } });
    });
    it('should leave committed source and diagnostics untouched', () => {
        context.reader.peekSignificant()!.content.should.equal('eventSource');
        context.diagnostics.should.have.lengthOf(0);
    });
    it('should reject a malformed concurrency header', () => {
        (captureConcurrency(context, splitLines('concurrency wrong')[0]) === undefined).should.be.true;
    });
});

describe('when a malformed reducer header still contains a rule', () => {
    let context: ParserContext;
    let captured: ReturnType<typeof captureReducer>;
    beforeEach(() => {
        context = new ParserContext(new LineReader(splitLines('reducer wrong\n  unknown\n    on NotARule\n  on E\n    ```csharp\non NotARule\n```')));
        captured = captureReducer(context, context.reader.takeSignificant());
    });
    it('should preserve the explicit rule without scanning skipped blocks or code', () => {
        captured.name.should.equal('');
        captured.readModel.should.equal('');
        captured.rules.map(rule => rule.event).should.deep.equal(['E']);
        context.reader.peekSignificant()!.content.should.equal('unknown');
        context.diagnostics.should.have.lengthOf(0);
    });
});

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
