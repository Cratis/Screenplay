// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PreviewFile } from './PreviewFile';
import { ServerDiagnostic } from './ServerDiagnostic';

export interface RepairPreview {
    token: string;
    binding?: { root: string; proposalId: string; beforeRevision: string; afterRevision: string; beforeEvidence: string; candidateEvidence: string };
    title: string;
    code: string;
    files: PreviewFile[];
    authoring: ServerDiagnostic[];
    executable: ServerDiagnostic[];
    executableReady: boolean;
    droppedComments: unknown[];
}
