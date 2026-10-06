// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import { spawnSync } from 'node:child_process';

/** Allow Windows PowerShell cold startup without weakening the fixture's ACL requirements. */
export const privateMetadataDirectoryTimeout = 60_000;

/** Test-owned metadata must satisfy the real server's owner-only permissions on every native OS. */
export function privateMetadataDirectory(directory: string): void {
    fs.mkdirSync(directory, { mode: 0o700 });
    if (process.platform !== 'win32') {
        fs.chmodSync(directory, 0o700);
        return;
    }
    // Node's POSIX mode cannot protect a Windows DACL. Use the same protected,
    // current-user-only inheritable access rule as McpFileAccess, never weaken it.
    // The synthetic path is data in the child environment, not interpolated code.
    const result = spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', [
        '$ErrorActionPreference = "Stop"',
        '$directory = [Environment]::GetEnvironmentVariable("SCREENPLAY_TEST_METADATA_DIRECTORY")',
        '$user = [Security.Principal.WindowsIdentity]::GetCurrent().User',
        '$access = New-Object Security.AccessControl.DirectorySecurity',
        '$access.SetAccessRuleProtection($true, $false)',
        '$rule = New-Object Security.AccessControl.FileSystemAccessRule($user, "FullControl", "ContainerInherit, ObjectInherit", "None", "Allow")',
        '$access.AddAccessRule($rule)',
        '[IO.Directory]::SetAccessControl($directory, $access)'
    ].join('; ')], { env: { ...process.env, SCREENPLAY_TEST_METADATA_DIRECTORY: directory }, encoding: 'utf8', timeout: privateMetadataDirectoryTimeout });
    if (result.error && 'code' in result.error && result.error.code === 'ETIMEDOUT') throw new Error(`Cannot protect test metadata directory: powershell.exe timed out after ${privateMetadataDirectoryTimeout} ms: ${result.error}`);
    if (result.error || result.status !== 0) throw new Error(`Cannot protect test metadata directory: ${result.error ?? result.stderr}`);
}
