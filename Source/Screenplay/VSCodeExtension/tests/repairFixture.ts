// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import * as fs from 'node:fs';

export const serverExecutable = process.env.SCREENPLAY_REPAIR_SERVER ?? path.resolve('../../DotNET/Tool/bin/Debug/net10.0/Cratis.Screenplay.Tool' + (process.platform === 'win32' ? '.exe' : ''));
export const serverAvailable = fs.existsSync(serverExecutable);
export const repairSource = 'policy IsAuthorized\n  file Handler.cs\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        projectId Uuid identifier\n        name String\n        produces Registered\n          name = name\n      event Registered\n        name String\n      command Anchor\n        anchorId Uuid identifier\n        produces Anchored\n          for anchorId\n      event Anchored\n';
export const missingEventSource = repairSource.replace('      event Registered\n        name String\n', '');
export const refusedEventSource = 'module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        projectId Uuid identifier\n        name String\n        produces Registered\n          name = name\n      specification Registers\n        when Register\n          name = "project"\n        then Registered\n          extra = "value"\n';
