// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseCaptureSource } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

// Frozen from the merge-base compiler: unmarked (Legacy) source keeps the pre-existing capture grammar,
// where a supplementary letter is a word character.
describe('when reading unmarked supplementary names in a capture', () => {
    const source = 'capture C\u{10400}\n  append E\u{10400}\n  children x\u{10400} identified by id\n    append F\n  nested n\u{10400}\n    append G\n';

    it('should keep the names in the Legacy wire output', () => {
        const capture = (toSyntaxJson(parseCaptureSource(source).value![0]) as Record<string, unknown>);
        (capture.name as string).should.equal('C\u{10400}');
        (capture.appends as { event: string }[]).map(append => append.event).should.deep.equal(['E\u{10400}']);
        (capture.children as { property: string; identifiedBy: string }[]).map(child => child.property).should.deep.equal(['x\u{10400}']);
        (capture.nested as { property: string }[]).map(child => child.property).should.deep.equal(['n\u{10400}']);
    });
});
