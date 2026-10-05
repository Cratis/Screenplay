// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type childProcess from 'node:child_process';

/**
 * Bounded test transport seam. Withholds the genuine, server-generated response frame of ONE
 * dispatched Apply request. `generated` settles only when the real server has produced that
 * response, which means installation already happened on disk. The response is never synthesized;
 * a test either releases the held bytes or kills its own server process to lose them.
 */
export function withholdApplyResponse(child: childProcess.ChildProcess, requestId: unknown, bound = 5_000): { generated: Promise<void>; release(): void } {
    const output = child.stdout!;
    const emit = output.emit;
    const chunks: Buffer[] = [];
    let held = 0, holding = true, buffered = '';
    let resolve!: () => void, reject!: (error: Error) => void;
    const generated = new Promise<void>((done, fail) => { resolve = done; reject = fail; });
    const timer = setTimeout(() => reject(new Error(`The real server did not generate its Apply response within ${bound} ms.`)), bound);
    output.emit = ((event: string | symbol, ...values: unknown[]) => {
        if (event !== 'data' || !holding) return Reflect.apply(emit, output, [event, ...values]);
        const bytes = Buffer.from(values[0] as Uint8Array);
        held += bytes.length;
        if (held > 4 * 1024 * 1024) { reject(new Error('Held Apply response exceeded its four-MiB bound.')); return true; }
        chunks.push(bytes);
        buffered += bytes.toString('utf8');
        for (const line of buffered.split('\n').slice(0, -1)) {
            try { if ((JSON.parse(line) as { id?: unknown }).id === requestId) { clearTimeout(timer); resolve(); } } catch { /* partial or non-JSON line */ }
        }
        buffered = buffered.slice(buffered.lastIndexOf('\n') + 1);
        return true;
    }) as typeof output.emit;
    return {
        generated,
        release: () => {
            clearTimeout(timer);
            holding = false;
            output.emit = emit;
            for (const chunk of chunks) Reflect.apply(emit, output, ['data', chunk]);
            chunks.length = 0;
        }
    };
}
