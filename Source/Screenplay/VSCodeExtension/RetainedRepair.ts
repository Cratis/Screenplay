// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairPreview } from './RepairPreview';
import { RepairSnapshot } from './RepairSnapshot';
import { RepairSourceSnapshot } from './RepairSourceSnapshot';

export interface RetainedRepair {
    preview: RepairPreview;
    snapshot: RepairSnapshot;
    sources: RepairSourceSnapshot;
    proposalId: string;
    beforeEvidence: string;
    candidateEvidence: string;
    afterRevision: string;
    afterCatalog: string;
    changeCount: number;
    dispatched: boolean;
}
