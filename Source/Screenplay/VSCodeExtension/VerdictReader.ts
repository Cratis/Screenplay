// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure, object, text, integer, array } from './RepairClient';
import { diagnostic } from './RepairSession';
import { RepairLocation } from './RepairLocation';
import { ServerDiagnostic } from './ServerDiagnostic';

const pageLimit = 200; // The diagnostics tool's maximum page size.
const maximumPages = 50;
const maximumItems = pageLimit * maximumPages;

/**
 * The codes the completeness checks report. The editor's ordinary validation never computes them,
 * and every other code in the MCP result is already shown by that validation, so only these appear.
 * PLAY0546 is not among them: it is a semantic binder warning, read separately from the executable diagnostics.
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

/**
 * Parses a finding of one of the wanted codes, or returns undefined for any other code without looking at its location:
 * the strict repair parser refuses positions of 0, which an application-level finding (PLAY0537 with no entry point)
 * legitimately carries together with no path.
 */
function verdictFrom(item: unknown, wanted: (code: string) => boolean): ServerDiagnostic | undefined {
    const value = object(item);
    if (typeof value.code !== 'string' || !wanted(value.code)) return undefined;
    const where = object(value.location);
    if (where.line === 0 && where.column === 0 && where.path === null) {
        const parsed = diagnostic({ ...value, location: { ...where, line: 1, column: 1 } });
        return { ...parsed, location: { line: 0, column: 0, path: null } satisfies RepairLocation };
    }
    return diagnostic(item);
}

export type VerdictTool = 'diagnostics' | 'open-workspace' | 'read-workspace';

/** One call of a read-only MCP tool. */
export type VerdictCall = (tool: VerdictTool, args: Record<string, unknown>) => Promise<Record<string, unknown>>;

/** The binder warning the diagnostics tool never reports; it comes from the executable diagnostics view. */
export const binderVerdictCode = 'PLAY0546';

/** Pages through every completeness finding, pinning the source revision the first page reported. */
export async function readVerdicts(call: VerdictCall): Promise<VerdictResult> {
    const result = await readCompleteness(args => call('diagnostics', args));
    // The binder warning rides along when the model binds. Any refusal there (for example, the model has errors) is silent.
    const binder = await readBinderVerdicts(call).catch(error => {
        if (error instanceof RepairFailure && error.kind === 'StaleEpoch') throw error;
        return [];
    });
    if (binder.length) {
        // The workspace and the diagnostics tool number revisions differently, so prove the source stayed put by pinning the first one again.
        const again = await call('diagnostics', { checks: 'all', limit: 1, offset: 0, expectedSourceRevision: result.revision });
        if (text(again.sourceRevision) !== result.revision) throw new RepairFailure('StaleSourceRevision', 'The source changed while the checks were read.');
    }
    return { ...result, diagnostics: [...result.diagnostics, ...binder] };
}

async function readBinderVerdicts(call: VerdictCall): Promise<ServerDiagnostic[]> {
    const opened = object((await call('open-workspace', {})).workspace);
    const revision = text(opened.revision), catalog = text(opened.catalogRevision);
    const found: ServerDiagnostic[] = [];
    let offset = 0;
    for (let pages = 0; pages < maximumPages; pages++) {
        const result = await call('read-workspace', { view: 'executable-diagnostics', offset, limit: 100, expectedRevision: revision, expectedCatalogRevision: catalog });
        const current = object(result.workspace);
        if (text(current.revision) !== revision || text(current.catalogRevision) !== catalog) throw new RepairFailure('StaleRevision', 'The workspace changed while the checks were read.');
        const page = object(result.page);
        if (integer(page.offset) !== offset || integer(page.totalCount) > maximumItems) throw new RepairFailure('MalformedContract', 'Executable diagnostics page mismatch.');
        const items = array(page.items);
        for (const item of items) {
            const parsed = verdictFrom(item, code => code === binderVerdictCode);
            if (parsed) found.push(parsed);
        }
        offset += items.length;
        if (page.nextOffset === null) return found;
        if (!items.length || integer(page.nextOffset) !== offset) throw new RepairFailure('MalformedContract', 'Nonprogressing executable diagnostics page.');
    }
    throw new RepairFailure('VerdictsTooLarge', 'Too many executable diagnostics pages.');
}

async function readCompleteness(call: (args: Record<string, unknown>) => Promise<Record<string, unknown>>): Promise<VerdictResult> {
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
            const parsed = verdictFrom(item, code => verdictCodes.has(code));
            if (parsed) collected.push(parsed);
        }
        offset += items.length;
        if (page.nextOffset === null) return { revision, status, diagnostics: collected };
        if (!items.length || integer(page.nextOffset) !== offset) throw new RepairFailure('MalformedContract', 'Nonprogressing diagnostics page.');
    }
    throw new RepairFailure('VerdictsTooLarge', 'Too many diagnostics pages.');
}
