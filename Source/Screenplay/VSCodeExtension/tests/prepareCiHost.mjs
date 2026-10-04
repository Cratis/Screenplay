// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { downloadAndUnzipVSCode } from '@vscode/test-electron';
import * as fs from 'node:fs';
import * as path from 'node:path';

// CI-only, explicit standard test-electron provisioning. Local test:host never downloads.
if (!process.env.GITHUB_ACTIONS || !process.env.GITHUB_ENV || !process.env.RUNNER_TEMP) throw new Error('Native runtime provisioning is CI-only. Set VSCODE_EXECUTABLE_PATH locally.');
const executable = await downloadAndUnzipVSCode({ version: '1.105.1', cachePath: path.join(process.env.RUNNER_TEMP, 'screenplay-vscode') });
fs.appendFileSync(process.env.GITHUB_ENV, `VSCODE_EXECUTABLE_PATH=${executable}\n`);
