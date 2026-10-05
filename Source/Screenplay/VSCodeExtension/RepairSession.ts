// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { randomUUID, createHash } from 'node:crypto';
import * as path from 'node:path';
import { RepairClient, RepairFailure, RepairLaunch, object, text, integer, boolean, array, evidence } from './RepairClient';

import { SavedVersions } from './SavedVersions';
import { RepairEnvironment } from './RepairEnvironment';
import { RepairLocation } from './RepairLocation';
import { ServerDiagnostic } from './ServerDiagnostic';
import { RepairChoice } from './RepairChoice';
import { RepairSnapshot } from './RepairSnapshot';
import { PreviewFile } from './PreviewFile';
import { RepairPreview } from './RepairPreview';
import { RetainedRepair } from './RetainedRepair';
export type { SavedVersions } from './SavedVersions';
export type { ServerDiagnostic } from './ServerDiagnostic';
export type { RepairPreview } from './RepairPreview';
import { observeRepair, observationHash, repairObservationEnabled, RepairObservation } from './RepairObservation';
const previewBudget = 16 * 1024 * 1024;

export function relativeFile(value: unknown): string {
    const file = text(value);
    if (!file || path.posix.isAbsolute(file) || file.includes('\\') || file.includes(':') || file.split('/').some(part => !part || part === '.' || part === '..')) {
        throw new RepairFailure('MalformedContract', 'Server returned an unsafe relative file path.');
    }
    return file;
}
export function location(value: unknown): RepairLocation {
    const item = object(value);
    const line = integer(item.line), column = integer(item.column);
    if (!line || !column) throw new RepairFailure('MalformedContract', 'Server source positions must be 1-based UTF-16.');
    return { line, column, path: item.path === null ? null : relativeFile(item.path) };
}
export function diagnostic(value: unknown): ServerDiagnostic {
    const item = object(value);
    const severity = text(item.severity);
    if (!['Error', 'Warning', 'Information'].includes(severity)) throw new RepairFailure('MalformedContract', 'Unsupported diagnostic severity.');
    return { code: text(item.code), message: text(item.message), severity, location: location(item.location) };
}
function revisions(value: unknown): { revision: string; catalog: string; executableReady: boolean } {
    const workspace = object(value);
    return { revision: text(workspace.revision), catalog: text(workspace.catalogRevision), executableReady: boolean(workspace.executableReady) };
}
function sameVersions(before: SavedVersions, after: SavedVersions): boolean {
    // Newly opened saved buffers are harmless; changed, closed or replaced original buffers are not.
    return Object.entries(before).every(([uri, version]) => after[uri] === version);
}

// All proposal authority is connection-local. Nothing takes a user-supplied proposal ID, AST or edit.
export class RepairSession {
    readonly #client: RepairClient;
    #epoch = 0;
    #snapshot?: RepairSnapshot;
    #retained?: RetainedRepair;
    #busy = false;
    #applying = false;
    #disposed = false;
    #uncertain = false;
    #available = true;
    #reconnectRequired = false;
    #discovery?: { epoch: number; versions: SavedVersions; promise: Promise<RepairSnapshot> };
    get reconnectRequired(): boolean { return this.#reconnectRequired; }
    get available(): boolean { return this.#available && !this.#disposed; }
    get epoch(): number { return this.#epoch; }
    get recoveryRequired(): boolean { return this.#uncertain; }
    /** A dispatched Apply request whose response has not settled; completed uncertain outcomes are not in flight. */
    get applyInFlight(): boolean { return this.#applying; }
    get applyDispatched(): boolean { return this.#retained?.dispatched === true; }

    #phase = 'idle';
    #proposal?: string;
    metadata(): Omit<RepairObservation, 'seq' | 'at' | 'source'> {
        if (!repairObservationEnabled(this.launch.root)) return {};
        return { ...this.observer, epoch: this.#epoch, capturedEpoch: this.#snapshot?.epoch, disposed: this.#disposed, phase: this.#phase, tokenHash: observationHash(this.#retained?.preview.token), proposalHash: observationHash(this.#proposal ?? this.#retained?.proposalId) };
    }
    trace(source: string, details: Omit<RepairObservation, 'seq' | 'at' | 'source'> = {}): void {
        if (repairObservationEnabled(this.launch.root)) observeRepair(this.launch.root, { source, ...this.metadata(), ...details });
    }
    constructor(readonly launch: RepairLaunch, readonly environment: RepairEnvironment, readonly changed: (cause: string) => void = () => {}, readonly observer: { owner: number; generation: number } = { owner: 0, generation: 0 }) {
        environment.check();
        this.#client = new RepairClient(launch, () => { this.#available = false; this.invalidate('client-close'); });
    }
    async initialize(): Promise<void> {
        try { await this.#client.initialize(); } catch (error) { this.dispose(); throw error; }
    }
    invalidate(cause = 'unspecified'): void {
        const before = this.#epoch;
        this.trace(`invalidate:${cause}:before`, { before });
        ++this.#epoch;
        this.#snapshot = undefined;
        if (!this.#retained?.dispatched) this.discard();
        this.changed(cause);
        this.trace(`invalidate:${cause}:after`, { before, after: this.#epoch });
    }
    dispose(cause = 'session-dispose'): void {
        this.trace(`dispose:${cause}`);
        this.#disposed = true;
        this.#client.close();
        this.invalidate(cause);
    }
    discard(): void {
        const retained = this.#retained;
        if (retained?.dispatched) return; // Retain uncertain outcome for inspection, not reuse.
        this.#retained = undefined;
        if (retained) void this.#client.tool('discard-proposal', { proposalId: retained.proposalId }).catch(() => {});
    }
    async inspectState(): Promise<Record<string, unknown>> {
        if (this.environment.checkRead) this.environment.checkRead();
        else this.environment.check();
        return this.#client.tool('workspace-state', { view: 'status' });
    }
    async discover(signal?: AbortSignal): Promise<{ choices: RepairChoice[]; diagnostics: ServerDiagnostic[] }> {
        if (signal?.aborted) throw new RepairFailure('Cancelled', 'Discovery consumer cancelled.');
        if (this.#uncertain || this.#retained) throw new RepairFailure('SessionBusy', 'Finish the current review, or inspect the uncertain apply outcome first.');
        if (this.#reconnectRequired) throw new RepairFailure('ReconnectRequired', 'Apply was dispatched. Deliberately reconnect after its outcome is classified before another repair.');
        const versions = this.environment.check();
        const epoch = this.#epoch;
        const pending = this.#discovery;
        const sameManifest = (saved: SavedVersions) => sameVersions(saved, versions) && sameVersions(versions, saved);
        if (pending?.epoch === epoch && sameManifest(pending.versions)) return this.#consumeDiscovery(pending.promise, epoch, versions, signal);
        if (this.#busy) throw new RepairFailure('SessionBusy', 'A repair operation is already active; rediscover after it finishes.');
        if (this.#snapshot && sameVersions(this.#snapshot.versions, versions)) return this.#snapshot;
        const operation = { epoch, versions, promise: this.#discover(epoch, versions) };
        this.#discovery = operation;
        void operation.promise.finally(() => { if (this.#discovery === operation) this.#discovery = undefined; }).catch(() => {});
        return this.#consumeDiscovery(operation.promise, epoch, versions, signal);
    }
    async #consumeDiscovery(promise: Promise<RepairSnapshot>, epoch: number, versions: SavedVersions, signal?: AbortSignal): Promise<RepairSnapshot> {
        // Cancelling one consumer never cancels another or the bounded underlying
        // read. The transport retains its deadline and drains every sent response.
        return new Promise((resolve, reject) => {
            const cancelled = () => { signal?.removeEventListener('abort', cancelled); reject(new RepairFailure('Cancelled', 'Discovery consumer cancelled.')); };
            signal?.addEventListener('abort', cancelled, { once: true });
            if (signal?.aborted) cancelled();
            void promise.then(result => {
                if (signal?.aborted) return;
                this.#check(epoch, versions);
                resolve(result);
            }).catch(reject).finally(() => signal?.removeEventListener('abort', cancelled));
        });
    }
    async #discover(epoch: number, versions: SavedVersions): Promise<RepairSnapshot> {
        this.#busy = true;
        this.#phase = 'discovery'; this.trace('discovery:begin', { capturedEpoch: epoch });
        try {
            const opened = revisions(await this.#client.tool('open-workspace', {}, undefined, () => this.#check(epoch, versions)));
            let pinnedEvidence: string | undefined;
            const read = async (view: string): Promise<unknown[]> => this.#items(async offset => {
                const result = await this.#client.tool('read-workspace', {
                    view, offset, limit: 100, expectedRevision: opened.revision, expectedCatalogRevision: opened.catalog,
                    ...(pinnedEvidence ? { expectedRepairEvidenceRevision: pinnedEvidence } : {}),
                }, undefined, () => this.#check(epoch, versions));
                this.#check(epoch, versions);
                const current = revisions(result.workspace);
                const pin = evidence(result.repairEvidenceRevision);
                if (current.revision !== opened.revision || current.catalog !== opened.catalog || (pinnedEvidence && pin !== pinnedEvidence)) throw new RepairFailure('StaleRevision', 'Discovery snapshot changed.');
                pinnedEvidence = pin;
                return object(result.page);
            }, opened.revision);
            const documents = new Map((await read('documents')).map(value => {
                const item = object(value);
                return [text(item.documentId), relativeFile(item.path)];
            }));
            const diagnostics = (await read('diagnostics')).map(diagnostic);
            const choices: RepairChoice[] = [];
            for (const value of await read('repairs')) {
                const item = object(value);
                if (!['PLAY0166', 'PLAY0478'].includes(String(item.diagnosticCode))) continue;
                const subject = object(item.subject);
                const source = location(item.location);
                const code = text(item.diagnosticCode);
                if (item.requiredFormatting !== 'CanonicalizeTouchedDocuments' || (code === 'PLAY0478' && item.canFixAll !== false)) throw new RepairFailure('MalformedContract', 'Repair metadata does not match the approved contract.');
                const handle = { revision: text(subject.revision), documentId: text(subject.documentId), path: text(subject.path) };
                if (handle.revision !== opened.revision || !source.path || documents.get(handle.documentId) !== source.path ||
                    !diagnostics.some(issue => issue.code === code && issue.location.path === source.path && issue.location.line === source.line && issue.location.column === source.column)) continue;
                choices.push({ token: randomUUID(), code, title: code === 'PLAY0478' ? 'Change routing: explicitly route to the command identifier' : 'Declare the missing produced event', location: source, subject: handle });
            }
            this.#check(epoch, versions);
            this.#snapshot = { epoch, versions, revision: opened.revision, catalog: opened.catalog, evidence: evidence(pinnedEvidence), diagnostics, choices };
            return this.#snapshot;
        } finally { this.#busy = false; this.#phase = 'idle'; this.trace('discovery:end'); }
    }

    // The caller obtains canonical-formatting consent BEFORE invoking this method.
    async preview(choiceToken: string, signal?: AbortSignal): Promise<RepairPreview> {
        if (this.#reconnectRequired) throw new RepairFailure('ReconnectRequired', 'Deliberately reconnect after the Apply outcome before reviewing another repair.');
        const snapshot = this.#snapshot;
        const choice = snapshot?.choices.find(item => item.token === choiceToken);
        if (!snapshot || !choice) throw new RepairFailure('StaleSelection', 'Rediscover this repair before reviewing.');
        this.#check(snapshot.epoch, snapshot.versions);
        if (this.#busy || this.#uncertain) throw new RepairFailure('SessionBusy', 'A repair operation is already active.');
        this.discard();
        this.#busy = true;
        this.#phase = 'preview-collection'; this.trace('preview:begin', { tokenHash: repairObservationEnabled(this.launch.root) ? observationHash(choiceToken) : undefined });
        let proposalId: string | undefined;
        try {
            const proposal = await this.#client.tool('propose-repair', {
                diagnosticCode: choice.code, subject: choice.subject, formatting: 'CanonicalizeTouchedDocuments',
                expectedRevision: snapshot.revision, expectedCatalogRevision: snapshot.catalog,
                pinRepairEvidence: true, expectedRepairEvidenceRevision: snapshot.evidence,
            }, signal, () => this.#check(snapshot.epoch, snapshot.versions));
            proposalId = text(proposal.proposalId);
            this.#proposal = proposalId; this.trace('preview:proposal');
            if (proposal.success !== true || proposal.validation !== 'Authoring') throw new RepairFailure('MalformedContract', 'Expected an accepted authoring proposal.');
            const before = revisions(proposal.before), after = revisions(proposal.after);
            const pins = object(proposal.repairEvidence);
            const beforeEvidence = evidence(pins.beforeRevision), candidateEvidence = evidence(pins.candidateRevision);
            if (before.revision !== snapshot.revision || before.catalog !== snapshot.catalog || beforeEvidence !== snapshot.evidence) throw new RepairFailure('StaleRevision', 'Proposal did not bind the discovered evidence.');
            const args = { proposalId, expectedRepairEvidenceRevision: beforeEvidence };
            const read = async (view: string): Promise<unknown[]> => this.#items(async offset => {
                const result = await this.#client.tool('read-proposal', { ...args, view, offset, limit: 100 }, signal);
                this.#verifyReview(result, before.revision, after.revision, beforeEvidence, candidateEvidence);
                return object(result.result);
            }, after.revision);
            const changes = await read('changes');
            if (changes.length !== integer(proposal.changeCount) || changes.length > 64) throw new RepairFailure('PreviewTooLarge', 'Incomplete or oversized change plan; Apply is disabled.');
            let bytes = 0;
            const files: PreviewFile[] = [];
            for (const value of changes) {
                const change = object(value);
                const documentId = text(change.documentId);
                const file: PreviewFile = { path: relativeFile(change.afterPath ?? change.beforePath), before: null, after: null };
                for (const side of ['before', 'after'] as const) {
                    const expectedBytes = change[`${side}Bytes`];
                    const expectedPath = change[`${side}Path`];
                    if (expectedBytes !== null) bytes += integer(expectedBytes);
                    if (bytes > previewBudget) throw new RepairFailure('PreviewTooLarge', 'Complete review exceeds 16 MiB; Apply is disabled.');
                    file[side] = await this.#bytePages(async offset => {
                        const result = await this.#client.tool('read-proposal', { ...args, view: side, documentId, offset, limit: 48 * 1024 }, signal);
                        this.#verifyReview(result, before.revision, after.revision, beforeEvidence, candidateEvidence);
                        const content = object(result.result);
                        if (boolean(content.exists) && relativeFile(content.path) !== expectedPath) throw new RepairFailure('MalformedContract', 'Preview path mismatch.');
                        return content;
                    }, side === 'before' ? before.revision : after.revision, expectedBytes === null ? null : integer(expectedBytes));
                }
                files.push(file);
            }
            const state = object(proposal.stateChange);
            if (relativeFile(state.path) !== '.screenplay/identities.json') throw new RepairFailure('MalformedContract', 'Unexpected identity-state path.');
            const identity: PreviewFile = { path: '.screenplay/identities.json', before: null, after: null };
            for (const side of ['before', 'after'] as const) {
                const count = integer(state[`${side}Bytes`]);
                bytes += count;
                if (bytes > previewBudget) throw new RepairFailure('PreviewTooLarge', 'Source and identity review exceed 16 MiB; Apply is disabled.');
                const revision = text(state[`${side}Revision`]);
                identity[side] = await this.#bytePages(async offset => this.#client.tool('workspace-state', { proposalId, view: side, expectedStateRevision: revision, offset, limit: 48 * 1024 }, signal), revision, side === 'before' && !boolean(state.beforeExists) ? null : count);
                const hash = identity[side] === null ? 'absent' : createHash('sha256').update(identity[side]!).digest('hex');
                if (hash !== revision) throw new RepairFailure('MalformedContract', 'Identity-state bytes do not match their revision.');
            }
            files.push(identity);
            const authoring = (await read('diagnostics')).map(diagnostic);
            const executable = (await read('executable-diagnostics')).map(diagnostic);
            const droppedComments = await read('dropped-comments');
            if (authoring.length !== integer(proposal.authoringDiagnosticCount) || droppedComments.length !== integer(proposal.droppedCommentCount)) throw new RepairFailure('MalformedContract', 'Incomplete diagnostic/comment review.');
            this.#check(snapshot.epoch, snapshot.versions);
            const preview: RepairPreview = { token: randomUUID(), binding: { root: this.launch.root, proposalId, beforeRevision: before.revision, afterRevision: after.revision, beforeEvidence, candidateEvidence }, title: choice.title, code: choice.code, files, authoring, executable, executableReady: after.executableReady, droppedComments };
            // Only a FULLY collected source AND state review obtains an apply token.
            this.#retained = { preview, snapshot, proposalId, beforeEvidence, candidateEvidence, afterRevision: after.revision, afterCatalog: after.catalog, changeCount: changes.length, dispatched: false };
            this.#phase = 'review'; this.trace('preview:retained');
            return preview;
        } catch (error) {
            if (proposalId) void this.#client.tool('discard-proposal', { proposalId }).catch(() => {});
            throw error;
        } finally { this.#busy = false; this.#proposal = undefined; if (!this.#retained) this.#phase = 'idle'; this.trace('preview:end'); }
    }

    async apply(token: string): Promise<void> {
        const retained = this.#retained;
        if (!retained || retained.preview.token !== token || retained.dispatched || this.#busy || this.#uncertain) throw new RepairFailure('UnauthorizedApply', 'Apply requires an outstanding complete preview from this session.');
        const snapshot = retained.snapshot;
        this.#check(snapshot.epoch, snapshot.versions);
        this.#busy = true;
        this.#applying = true;
        try {
            const result = await this.#client.tool('apply', {
                proposalId: retained.proposalId, expectedRevision: snapshot.revision, expectedCatalogRevision: snapshot.catalog,
                expectedRepairEvidenceRevision: retained.beforeEvidence,
            }, undefined, () => this.#check(snapshot.epoch, snapshot.versions), () => {
                retained.dispatched = true; // Write attempt: no cancellation or retry after this boundary.
                this.#reconnectRequired = true; // Barrier starts at dispatch, not an incidental rename.
                this.invalidate('apply-dispatch'); // Retain the dispatched record, but expire all old review authority.
            });
            const installed = revisions(result.workspace);
            if (result.success !== true || result.validation !== 'Authoring' || installed.revision !== retained.afterRevision || installed.catalog !== retained.afterCatalog || integer(result.plannedChanges) !== retained.changeCount || integer(result.installedDocuments) !== retained.changeCount) throw new RepairFailure('ApplyOutcomeUnknown', 'Apply did not report a verified installation.', result);
            this.#retained = undefined;
            this.invalidate('apply-installed');
        } catch (error) {
            if (retained.dispatched) {
                this.#uncertain = true;
                throw new RepairFailure('ApplyOutcomeUnknown', 'Apply was dispatched. Changes may exist. Do not retry; inspect workspace-state and review recovery separately.', error);
            }
            throw error;
        } finally { this.#busy = false; this.#applying = false; }
    }

    #check(epoch: number, versions: SavedVersions): void {
        if (this.#disposed || epoch !== this.#epoch) {
            this.trace(this.#disposed ? 'guard:disposed' : 'guard:epoch', { capturedEpoch: epoch });
            throw new RepairFailure('StaleEpoch', 'The workspace changed. Rediscover and review a fresh repair.');
        }
        if (!sameVersions(versions, this.environment.check())) {
            this.trace('guard:saved-versions', { capturedEpoch: epoch });
            throw new RepairFailure('StaleBuffer', 'A saved buffer version changed. Rediscover this repair.');
        }
    }
    #verifyReview(value: Record<string, unknown>, before: string, after: string, base: string, candidate: string): void {
        const pins = object(value.repairEvidence);
        if (revisions(value.before).revision !== before || revisions(value.after).revision !== after || evidence(pins.beforeRevision) !== base || evidence(pins.candidateRevision) !== candidate) throw new RepairFailure('StaleRevision', 'Retained preview evidence changed.');
    }
    async #items(read: (offset: number) => Promise<Record<string, unknown>>, revision: string): Promise<unknown[]> {
        const result: unknown[] = [];
        let collectedBytes = 0;
        let total: number | undefined;
        do {
            const page = await read(result.length);
            if (text(page.revision) !== revision || integer(page.offset) !== result.length) throw new RepairFailure('MalformedContract', 'Item page revision/offset mismatch.');
            const size = integer(page.totalCount);
            if (size > 10_000 || (total !== undefined && size !== total)) throw new RepairFailure('PreviewTooLarge', 'Item pages exceed the complete-review budget.');
            total = size;
            const items = array(page.items);
            collectedBytes += Buffer.byteLength(JSON.stringify(items), 'utf8');
            if (collectedBytes > previewBudget) throw new RepairFailure('PreviewTooLarge', 'Item pages exceed the 16 MiB metadata-cache budget; complete review is refused.');
            if (items.length > 100 || result.length + items.length > size) throw new RepairFailure('MalformedContract', 'Invalid item page length.');
            result.push(...items);
            if (page.nextOffset === null) {
                if (result.length !== size) throw new RepairFailure('MalformedContract', 'Truncated item pages.');
                return result;
            }
            if (!items.length || integer(page.nextOffset) !== result.length) throw new RepairFailure('MalformedContract', 'Nonprogressing item page.');
        } while (result.length < 10_000);
        throw new RepairFailure('PreviewTooLarge', 'Too many item pages.');
    }
    async #bytePages(read: (offset: number) => Promise<Record<string, unknown>>, revision: string, count: number | null): Promise<Buffer | null> {
        const chunks: Buffer[] = [];
        let offset = 0;
        do {
            const content = await read(offset);
            if (!boolean(content.exists)) {
                if (count !== null || offset) throw new RepairFailure('MalformedContract', 'Missing reviewed bytes.');
                return null;
            }
            if (count === null || count > previewBudget) throw new RepairFailure('PreviewTooLarge', 'Unexpected or oversized reviewed bytes.');
            const page = object(content.content);
            const encoded = text(page.bytesBase64);
            if (encoded.length > 4 * 64 * 1024 || !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(encoded)) throw new RepairFailure('MalformedContract', 'Invalid byte page base64.');
            const bytes = Buffer.from(encoded, 'base64');
            if (text(page.revision) !== revision || integer(page.totalBytes) !== count || integer(page.offset) !== offset || integer(page.byteCount) !== bytes.length || offset + bytes.length > count) throw new RepairFailure('MalformedContract', 'Byte page binding/length mismatch.');
            chunks.push(bytes);
            offset += bytes.length;
            if (page.nextOffset === null) {
                if (offset !== count) throw new RepairFailure('MalformedContract', 'Truncated byte preview; Apply disabled.');
                const complete = Buffer.concat(chunks);
                try { new TextDecoder('utf-8', { fatal: true }).decode(complete); }
                catch { throw new RepairFailure('MalformedContract', 'Reviewed source or identity bytes are not strict UTF-8; Apply is disabled.'); }
                return complete;
            }
            if (!bytes.length || integer(page.nextOffset) !== offset) throw new RepairFailure('MalformedContract', 'Nonprogressing byte page.');
        } while (offset <= previewBudget);
        throw new RepairFailure('PreviewTooLarge', 'Preview byte budget exceeded.');
    }
}
