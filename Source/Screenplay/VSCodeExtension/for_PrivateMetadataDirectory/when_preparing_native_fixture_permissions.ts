// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, expect, it, vi } from 'vitest';

const host = vi.hoisted(() => ({ mkdir: vi.fn(), chmod: vi.fn(), spawn: vi.fn() }));
vi.mock('node:fs', () => ({ mkdirSync: host.mkdir, chmodSync: host.chmod }));
vi.mock('node:child_process', () => ({ spawnSync: host.spawn }));
import { privateMetadataDirectory } from '../tests/privateMetadataDirectory';

beforeEach(() => { vi.clearAllMocks(); host.spawn.mockReturnValue({ status: 0 }); });
afterEach(() => vi.unstubAllGlobals());

it('keeps POSIX test metadata private without invoking a Windows process', () => {
    vi.stubGlobal('process', { ...process, platform: 'darwin' });
    privateMetadataDirectory('/synthetic/.screenplay');
    expect(host.mkdir).toHaveBeenCalledWith('/synthetic/.screenplay', { mode: 0o700 });
    expect(host.chmod).toHaveBeenCalledWith('/synthetic/.screenplay', 0o700);
    expect(host.spawn).not.toHaveBeenCalled();
});

it('protects Windows metadata with only the current-user inheritable DACL and a data-only path', () => {
    vi.stubGlobal('process', { ...process, platform: 'win32' });
    const directory = 'C:\\synthetic path\\.screenplay';
    privateMetadataDirectory(directory);
    const [executable, arguments_, options] = host.spawn.mock.calls[0];
    expect(executable).toBe('powershell.exe');
    expect(arguments_.slice(0, 3)).toEqual(['-NoProfile', '-NonInteractive', '-Command']);
    expect(arguments_[3]).toContain('$access.SetAccessRuleProtection($true, $false)');
    expect(arguments_[3]).toContain('[Security.Principal.WindowsIdentity]::GetCurrent().User');
    expect(arguments_[3]).toContain('"ContainerInherit, ObjectInherit", "None", "Allow"');
    expect(arguments_[3]).toContain('[IO.Directory]::SetAccessControl($directory, $access)');
    expect(arguments_[3]).not.toContain(directory);
    expect(options.env.SCREENPLAY_TEST_METADATA_DIRECTORY).toBe(directory);
    expect(options.timeout).toBe(5_000);
    expect(host.chmod).not.toHaveBeenCalled();
});

for (const result of [{ status: 1, stderr: 'ACL refused' }, { status: null, error: new Error('timed out') }]) it('fails closed if native permission setup fails or times out', () => {
    vi.stubGlobal('process', { ...process, platform: 'win32' });
    host.spawn.mockReturnValue(result);
    expect(() => privateMetadataDirectory('C:\\synthetic\\.screenplay')).toThrow('Cannot protect test metadata directory');
});
