// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure, object, text, integer, array } from './RepairClient';
import { diagnostic } from './RepairSession';
import { ServerDiagnostic } from './ServerDiagnostic';

const pageLimit = 200; // The diagnostics tool's maximum page size.
const maximumPages = 50;
const maximumItems = pageLimit * maximumPages;

/**
 * The codes the completeness checks report. The editor's ordinary validation never computes them,
 * and every other code in the MCP result is already shown by that validation, so only these appear.
 * PLAY0546 is not among them: the diagnostics tool does not run the semantic binder.
 */
export const verdictCodes: ReadonlySet<string> = new Set([
    'PLAY0530', 'PLAY0531', 'PLAY0532', 'PLAY0533', 'PLAY0534', 'PLAY0535', 'PLAY0536', 'PLAY0537',
    'PLAY0574', 'PLAY0575', 'PLAY0576', 'PLAY0602', 'PLAY0603', 'PLAY0604', 'PLAY0605', 'PLAY0606',
    'PLAY0652', 'PLAY0671',
]);

export interface VerdictResult {
    readonly revision: string;
    /** Why the checks did not run (for example, the model has errors), or null when they did. */
    readonly status: string | null;
    readonly diagnostics: ServerDiagnostic[];
}

/** One call of the MCP `diagnostics` tool. */
export type DiagnosticsCall = (args: Record<string, unknown>) => Promise<Record<string, unknown>>;

/** Pages through every completeness finding, pinning the source revision the first page reported. */
export async function readVerdicts(call: DiagnosticsCall): Promise<VerdictResult> {
    const collected: ServerDiagnostic[] = [];
    let revision: string | undefined;
    let status: string | null = null;
    let offset = 0;
    for (let pages = 0; pages < maximumPages; pages++) {
        const result = await call({ checks: 'all', limit: pageLimit, offset, ...(revision ? { expectedSourceRevision: revision } : {}) });
        const page = object(result.page);
        const current = text(result.sourceRevision);
        if (revision !== undefined && current !== revision) throw new RepairFailure('StaleSourceRevision', 'The source changed while the checks were read.');
        revision = current;
        if (text(page.revision) !== revision || integer(page.offset) !== offset) throw new RepairFailure('MalformedContract', 'Diagnostics page revision/offset mismatch.');
        if (integer(page.totalCount) > maximumItems) throw new RepairFailure('VerdictsTooLarge', `More than ${maximumItems} findings; narrow the model before checking.`);
        if (typeof result.completenessStatus === 'string') status = result.completenessStatus;
        const items = array(page.items);
        for (const item of items) {
            const parsed = diagnostic(item);
            if (verdictCodes.has(parsed.code)) collected.push(parsed);
        }
        offset += items.length;
        if (page.nextOffset === null) return { revision, status, diagnostics: collected };
        if (!items.length || integer(page.nextOffset) !== offset) throw new RepairFailure('MalformedContract', 'Nonprogressing diagnostics page.');
    }
    throw new RepairFailure('VerdictsTooLarge', 'Too many diagnostics pages.');
}
