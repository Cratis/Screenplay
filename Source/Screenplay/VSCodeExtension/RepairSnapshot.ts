// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SavedVersions } from './SavedVersions';
import { ServerDiagnostic } from './ServerDiagnostic';
import { RepairChoice } from './RepairChoice';

export interface RepairSnapshot {
    epoch: number;
    versions: SavedVersions;
    revision: string;
    catalog: string;
    evidence: string;
    diagnostics: ServerDiagnostic[];
    choices: RepairChoice[];
}
