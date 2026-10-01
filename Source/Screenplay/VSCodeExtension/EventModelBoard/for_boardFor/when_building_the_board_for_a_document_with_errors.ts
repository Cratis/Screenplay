// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { ShowBoardMessage } from '../../Webview/BoardMessage';
import { boardFor } from '../compileBoard';

describe('when building the board for a document with errors', () => {
    let board: ShowBoardMessage;

    beforeEach(() => {
        board = boardFor(parse([
            'module Projects',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      event ProjectRegistered',
            '        name String',
            'modul Typo',
        ].join('\n'), 'Projects/Projects.play'), 'Projects');
    });

    it('should still draw what could be read', () => {
        (board.document as { collections: { modules: { name: string }[] }[] }).collections[0].modules.map(module => module.name).should.deep.equal(['Projects']);
    });

    it('should name the document after the file', () => {
        (board.document as { name: string }).name.should.equal('Projects');
    });

    it('should carry the problem with where it is', () => {
        board.problems.should.deep.equal([{ severity: 'error', code: 'PLAY0001', message: board.problems[0].message, line: 6, path: 'Projects/Projects.play' }]);
    });
});
