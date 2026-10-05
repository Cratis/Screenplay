// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as path from 'node:path';
import type { SpawnOptions } from 'node:child_process';
import { rootReplacementLaunchOptions } from '../tests/nativeRootReplacement';

describe('when launching the root replacement fixture', () => {
    const root = 'C:\\synthetic\\screenplay-repair-host';
    const server = 'C:\\approved\\Cratis.Screenplay.Tool.exe';
    const options: SpawnOptions = { cwd: path.win32.join(root, 'root-replacement'), shell: false, windowsHide: true, stdio: 'pipe' };

    it('moves only the actual Windows replacement child CWD to its fixture parent', () => {
        expect(rootReplacementLaunchOptions(server, options, server, root, 'win32')).toEqual({ ...options, cwd: root });
        expect(options.cwd).toBe(path.win32.join(root, 'root-replacement'));
    });

    it('preserves the normal launch on POSIX', () => {
        expect(rootReplacementLaunchOptions(server, options, server, root, 'darwin')).toBe(options);
        expect(rootReplacementLaunchOptions(server, options, server, root, 'linux')).toBe(options);
    });

    it('never changes another executable or another fixture', () => {
        expect(rootReplacementLaunchOptions('C:\\other.exe', options, server, root, 'win32')).toBe(options);
        const other = { ...options, cwd: path.win32.join(root, 'post-dispatch') };
        expect(rootReplacementLaunchOptions(server, other, server, root, 'win32')).toBe(other);
        const sibling = { ...options, cwd: path.win32.join(root, 'root-replacement-next') };
        expect(rootReplacementLaunchOptions(server, sibling, server, root, 'win32')).toBe(sibling);
    });

    it('does not supply options to an unrelated spawn without them', () => {
        expect(rootReplacementLaunchOptions(server, undefined, server, root, 'win32')).toBeUndefined();
    });
});
