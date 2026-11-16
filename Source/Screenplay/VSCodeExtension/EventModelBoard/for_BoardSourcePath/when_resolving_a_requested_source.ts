// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import * as vscode from 'vscode';
import { resolveBoardSource } from '../BoardSourcePath';

const root = vscode.Uri.file('/work/app');
const compiled: ReadonlySet<string> = new Set(['application.play', 'slices/a.play', '../shared/types.play']);

const hostile: readonly [string, unknown][] = [
    ['a parent folder file', '../outside.play'],
    ['a deep parent traversal', '../../../etc/passwd'],
    ['a traversal in the middle', 'a/../../x.play'],
    ['an absolute path', '/etc/passwd'],
    ['a drive path', 'C:\\Windows\\x.play'],
    ['a network share', '\\\\server\\share\\x.play'],
    ['a backslash traversal', 'a\\..\\..\\x.play'],
    ['an encoded traversal', '%2e%2e/x.play'],
    ['a file uri', 'file:///etc/passwd'],
    ['a null byte', 'sub/x.play\0.txt'],
    ['an empty string', ''],
    ['a number', 42],
    ['an object', { path: 'slices/a.play' }],
    ['null', null],
    ['undefined', undefined],
    ['a play file that was not compiled', 'slices/b.play'],
    ['a case variant of a compiled file', 'Slices/A.play'],
    ['a prefix of a compiled file', 'slices/a'],
    ['a compiled file with a trailing slash', 'slices/a.play/'],
    ['a compiled file with a dot prefix', './slices/a.play'],
];

describe('when resolving a requested source', () => {
    for (const [description, requested] of hostile) {
        it(`should refuse ${description}`, () => {
            (resolveBoardSource(root, compiled, requested) === undefined).should.be.true;
        });
    }

    it('should refuse a non play file even when it is listed', () => {
        (resolveBoardSource(root, new Set(['notes.txt']), 'notes.txt') === undefined).should.be.true;
    });

    it('should refuse everything when the board has no root', () => {
        (resolveBoardSource(undefined, compiled, 'slices/a.play') === undefined).should.be.true;
    });

    it('should resolve a compiled file beneath the root', () => {
        resolveBoardSource(root, compiled, 'slices/a.play')!.fsPath.should.equal('/work/app/slices/a.play');
    });

    it('should resolve a compiled file reached by an import that climbs out of the folder', () => {
        resolveBoardSource(root, compiled, '../shared/types.play')!.fsPath.should.equal('/work/app/../shared/types.play');
    });
});
