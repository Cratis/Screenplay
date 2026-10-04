// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairPreview } from './RepairPreview';
import { RepairSnapshot } from './RepairSnapshot';

export interface RetainedRepair {
    preview: RepairPreview;
    snapshot: RepairSnapshot;
    proposalId: string;
    beforeEvidence: string;
    candidateEvidence: string;
    afterRevision: string;
    afterCatalog: string;
    changeCount: number;
    dispatched: boolean;
}
