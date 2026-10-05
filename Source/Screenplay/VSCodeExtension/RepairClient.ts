// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn, ChildProcessWithoutNullStreams } from 'node:child_process';
import { isAbsolute } from 'node:path';
import { RepairLaunch } from './RepairLaunch';
import { RepairRequest } from './RepairRequest';
export type { RepairLaunch } from './RepairLaunch';

export class RepairFailure extends Error {
    constructor(readonly kind: string, message: string, readonly details?: unknown) { super(message); }
}

export function object(value: unknown): Record<string, unknown> {
    if (!value || typeof value !== 'object' || Array.isArray(value)) throw new RepairFailure('MalformedContract', 'Expected a contract object.');
    return value as Record<string, unknown>;
}
export function text(value: unknown): string {
    if (typeof value !== 'string') throw new RepairFailure('MalformedContract', 'Expected a contract string.');
    return value;
}
export function integer(value: unknown): number {
    if (!Number.isSafeInteger(value) || (value as number) < 0) throw new RepairFailure('MalformedContract', 'Expected a nonnegative integer.');
    return value as number;
}
export function boolean(value: unknown): boolean {
    if (typeof value !== 'boolean') throw new RepairFailure('MalformedContract', 'Expected a boolean.');
    return value;
}
export function array(value: unknown): unknown[] {
    if (!Array.isArray(value)) throw new RepairFailure('MalformedContract', 'Expected a contract array.');
    return value;
}
export function evidence(value: unknown): string {
    const revision = text(value);
    if (!/^re1:[a-f0-9]{64}$/.test(revision)) throw new RepairFailure('MalformedContract', 'Missing or unsupported repair evidence.');
    return revision;
}
// Deliberately not a general MCP host: no roots, sampling, agents, or write automation.
export class RepairClient {
    readonly #process: ChildProcessWithoutNullStreams;
    readonly #queue: RepairRequest[] = [];
    #active?: RepairRequest;
    #nextId = 0;
    #bytes = Buffer.alloc(0);
    #stderr = Buffer.alloc(0);
    #closed?: RepairFailure;
    readonly #frameBytes = 3 * 1024 * 1024; // JSON-RPC includes structured AND text copies.

    constructor(launch: RepairLaunch, readonly onClose: (failure: RepairFailure) => void = () => {}, readonly timeoutMs = 60_000) {
        if (!isAbsolute(launch.executable) || !isAbsolute(launch.root) || launch.arguments.some(arg => typeof arg !== 'string' || arg.includes('\0'))) {
            throw new RepairFailure('ConfigurationRefused', 'Configure an absolute executable and physical model root, with separate argument strings.');
        }
        this.#process = spawn(launch.executable, [...launch.arguments, launch.root], { shell: false, cwd: launch.root, windowsHide: true, stdio: 'pipe' });
        this.#process.stdout.on('data', (chunk: Buffer) => this.#receive(chunk));
        this.#process.stderr.on('data', (chunk: Buffer) => { this.#stderr = Buffer.concat([this.#stderr, chunk]).subarray(-16 * 1024); });
        this.#process.on('error', error => this.close(new RepairFailure('ProcessUnavailable', `Cannot start the configured repair server: ${error.message}`)));
        this.#process.stdin.on('error', error => this.close(new RepairFailure('ProcessClosed', error.message)));
        this.#process.stdout.on('end', () => this.close(new RepairFailure('ProcessClosed', 'Repair server output ended.')));
        this.#process.on('exit', () => this.close(new RepairFailure('ProcessClosed', 'Repair server exited.')));
    }

    async initialize(): Promise<void> {
        const initialized = object(await this.request('initialize', { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'screenplay-vscode-repairs', version: '1' } }));
        if (initialized.protocolVersion !== '2025-06-18') throw new RepairFailure('UnsupportedContract', 'Repair server must support MCP 2025-06-18.');
        text(object(initialized.serverInfo).version); // Identification only, never compatibility proof.
        this.#process.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) + '\n');
        const listed = object(await this.request('tools/list', {}));
        const names = new Set(array(listed.tools).map(tool => text(object(tool).name)));
        for (const required of ['repair-capabilities', 'open-workspace', 'read-workspace', 'propose-repair', 'read-proposal', 'workspace-state', 'discard-proposal', 'apply']) {
            if (!names.has(required)) throw new RepairFailure('UnsupportedContract', `Repair server lacks ${required}. Install a release containing repair contract v1.`);
        }
        validateCapabilities(await this.tool('repair-capabilities', {}));
    }

    async tool(name: string, args: Record<string, unknown>, signal?: AbortSignal, beforeSend?: () => void, writeAttempt?: () => void): Promise<Record<string, unknown>> {
        const envelope = object(await this.request('tools/call', { name, arguments: args }, signal, beforeSend, name === 'propose-repair' ? value => {
            const late = object(object(value).structuredContent);
            if (late.success === true && typeof late.proposalId === 'string') void this.tool('discard-proposal', { proposalId: late.proposalId }).catch(() => {});
        } : undefined, writeAttempt));
        const result = object(envelope.structuredContent);
        if (boolean(envelope.isError)) {
            // Unknown failures after an apply are conservatively interpreted by the session, not by text prefixes.
            throw new RepairFailure(text(result.failureKind), `Repair server refused: ${String(result.message ?? result.status ?? result.failureKind)}`, result);
        }
        return result;
    }

    request(method: string, params: unknown, signal?: AbortSignal, beforeSend?: () => void, discarded?: (value: unknown) => void, writeAttempt?: () => void): Promise<unknown> {
        if (this.#closed) return Promise.reject(this.#closed);
        if (signal?.aborted) return Promise.reject(new RepairFailure('Cancelled', 'Repair request cancelled before dispatch.'));
        if (this.#queue.length >= 16) return Promise.reject(new RepairFailure('QueueFull', 'Repair request queue is full.'));
        return new Promise((resolve, reject) => {
            const request: RepairRequest = { id: ++this.#nextId, method, params, resolve, reject, cancelled: false, signal, beforeSend, discarded, writeAttempt };
            request.abort = () => {
                const queued = this.#queue.indexOf(request);
                request.cancelled = true;
                if (queued >= 0) { this.#queue.splice(queued, 1); this.#release(request); }
                // The server ignores cancellation. Drain the in-flight response within its original deadline.
                reject(new RepairFailure('Cancelled', 'Read cancelled; any in-flight server response will be discarded.'));
            };
            signal?.addEventListener('abort', request.abort, { once: true });
            this.#queue.push(request);
            this.#pump();
        });
    }

    close(failure = new RepairFailure('ProcessClosed', 'Repair connection closed.')): void {
        if (this.#closed) return;
        this.#closed = failure;
        const pending = [...this.#queue];
        this.#queue.length = 0;
        if (this.#active) pending.push(this.#active);
        this.#active = undefined;
        for (const request of pending) { this.#release(request); request.reject(failure); }
        this.#bytes = Buffer.alloc(0);
        this.#process.stdin.destroy();
        this.#process.kill();
        const kill = setTimeout(() => { if (this.#process.exitCode === null && this.#process.signalCode === null) this.#process.kill('SIGKILL'); }, 1_000);
        kill.unref();
        this.onClose(failure);
    }

    #pump(): void {
        if (this.#active || this.#closed) return;
        const request = this.#queue.shift();
        if (!request) return;
        let frame: string;
        try {
            // Serialization can throw or run caller code. It must precede final authorization.
            frame = JSON.stringify({ jsonrpc: '2.0', id: request.id, method: request.method, params: request.params }) + '\n';
            request.beforeSend?.();
        } catch (error) { this.#release(request); request.reject(error); this.#pump(); return; }
        this.#active = request;
        request.timer = setTimeout(() => this.close(new RepairFailure('DeadlineExceeded', 'Repair server deadline exceeded.')), this.timeoutMs);
        try {
            request.writeAttempt?.(); // May have been sent, NOT acknowledgement or installation.
            this.#process.stdin.write(frame);
        } catch (error) { this.close(new RepairFailure('ProcessClosed', `Repair write failed: ${String(error)}`)); }
    }

    #receive(chunk: Buffer): void {
        if (this.#closed) return;
        try {
            // Process coalesced frames without accumulating an unbounded chunk with the pending partial frame.
            let start = 0;
            while (start < chunk.length) {
                const end = chunk.indexOf(10, start);
                const piece = chunk.subarray(start, end < 0 ? chunk.length : end);
                if (this.#bytes.length + piece.length > this.#frameBytes) throw new RepairFailure('FrameTooLarge', 'Repair response exceeded the frame limit.');
                this.#bytes = Buffer.concat([this.#bytes, piece]);
                if (end < 0) break;
                const frame = object(JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(this.#bytes)));
                this.#bytes = Buffer.alloc(0);
                const request = this.#active;
                if (frame.jsonrpc !== '2.0' || !request || frame.id !== request.id || (('error' in frame) === ('result' in frame)) || 'method' in frame) {
                    throw new RepairFailure('UnexpectedFrame', 'Unexpected repair response or request ID.');
                }
                const failure = 'error' in frame ? object(frame.error) : undefined;
                const typed = failure ? new RepairFailure(text(object(failure.data).failureKind), text(failure.message), failure.data) : undefined;
                this.#active = undefined;
                this.#release(request);
                if (!request.cancelled) {
                    if (typed) request.reject(typed);
                    else request.resolve(frame.result);
                } else if (!typed) request.discarded?.(frame.result);
                // A second unsolicited frame must not be associated with an as-yet unsent request.
                start = end + 1;
            }
            this.#pump();
        } catch (error) {
            this.close(error instanceof RepairFailure ? error : new RepairFailure('MalformedFrame', 'Malformed UTF-8/JSON repair response.'));
        }
    }

    #release(request: RepairRequest): void {
        if (request.timer) clearTimeout(request.timer);
        if (request.abort) request.signal?.removeEventListener('abort', request.abort);
    }
}

export function validateCapabilities(value: unknown): void {
    const caps = object(value);
    if (caps.schema !== 'cratis.screenplay.mcp.repair-capabilities' || caps.schemaVersion !== 1 || caps.repairContractVersion !== 1 ||
        caps.savedFilesOnly !== true || caps.journaledApply !== true || object(caps.evidence).version !== 1 || object(caps.evidence).revisionPrefix !== 're1:' ||
        object(caps.structuredFailures).version !== 1 || object(caps.structuredFailures).discriminator !== 'failureKind' || object(caps.cancellation).supported !== false) {
        throw new RepairFailure('UnsupportedContract', 'This server lacks the required saved-file, evidence-pinned repair contract v1. No unsafe fallback is available.');
    }
    text(caps.serverVersion);
    for (const code of ['PLAY0166', 'PLAY0478']) {
        const action = array(caps.actions).map(object).find(action => action.diagnosticCode === code);
        if (!action || action.tool !== 'propose-repair' || action.pinRepairEvidence !== true || action.requiredFormatting !== 'CanonicalizeTouchedDocuments') {
            throw new RepairFailure('UnsupportedContract', `Server lacks the required pinned ${code} action.`);
        }
    }
    const limits = object(caps.limits);
    for (const name of ['retainedProposals', 'bytePageBytes', 'itemPageItems', 'structuredResponseBytes', 'requestCharacters']) {
        if (integer(limits[name]) < 1) throw new RepairFailure('MalformedContract', 'Invalid repair limits.');
    }
}
