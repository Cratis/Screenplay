// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '@cratis/screenplay-event-models';
import { boardFor } from '../compileBoard';
import { narrowTo } from '../boardScope';
import { ShowBoardMessage } from '../../Webview/BoardMessage';

const compilation = parse('module Views\n  feature F\n    slice StateView View\n      readmodel P\n      projection P\n        from E\nmodule Facts\n  feature F\n    slice StateChange Write\n      event E', 'work.play');

describe('when annotating a narrowed board', () => {
    let board: ShowBoardMessage;
    let document: EventModelDocument;

    beforeEach(() => {
        const narrowed = narrowTo(compilation.value, { files: new Set(), containers: [['Views']] })!;
        board = boardFor({ ...compilation, value: narrowed }, 'Views', compilation.value);
        document = board.document as EventModelDocument;
    });

    it('should keep the whole application finding when its producer is outside the board', () => {
        document.collections[0].modules.should.have.lengthOf(1);
        document.collections[0].modules[0].features[0].slices[0].annotations!.should.deep.equal([{
            severity: 'information', code: 'PLAY0516', message: compilation.diagnostics[0].message,
        }]);
    });

    it('should keep the existing problem and source location', () => {
        board.problems.should.deep.equal([{ severity: 'information', code: 'PLAY0516', message: compilation.diagnostics[0].message, line: compilation.diagnostics[0].location.line, path: 'work.play' }]);
    });

    it('should not leak a finding to a board without its consumer', () => {
        const narrowed = narrowTo(compilation.value, { files: new Set(), containers: [['Facts']] })!;
        const other = boardFor({ ...compilation, value: narrowed }, 'Facts', compilation.value).document as EventModelDocument;
        (other.collections[0].modules[0].features[0].slices[0].annotations === undefined).should.be.true;
    });

    it('should not invent a finding when the compilation suppressed it', () => {
        const suppressed = boardFor({ ...compilation, diagnostics: [] }, 'Work', compilation.value).document as EventModelDocument;
        (suppressed.collections[0].modules[0].features[0].slices[0].annotations === undefined).should.be.true;
    });
});
