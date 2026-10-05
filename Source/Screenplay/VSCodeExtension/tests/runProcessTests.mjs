// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
const result = spawnSync(process.execPath, ['../../../node_modules/vitest/vitest.mjs', 'run', 'for_RepairProcess'], { stdio: 'inherit', env: { ...process.env, SCREENPLAY_REQUIRE_REPAIR_SERVER: '1' } });
if (result.error) throw result.error;
process.exit(result.status ?? 1);
