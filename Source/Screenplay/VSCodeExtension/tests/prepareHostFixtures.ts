// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import * as path from 'node:path';
import { repairSource, refusedEventSource } from './repairFixture';

/** Launcher-only: all baseline writes complete BEFORE the native host is started. */
export const associatedUntitledTargets = ['sibling.play', 'pending-handler.cs', '.screenplay/pending-identities.json'] as const;

export function prepareHostFixtures(root: string): void {
    // Each independent application is a sibling, never nested inside the first
    // approved application (the server recursively discovers that application's sources).
    const direct = path.join(root, 'direct');
    fs.mkdirSync(direct);
    fs.writeFileSync(path.join(direct, 'application.play'), '\uFEFF// 😀 native byte review\r\n' + repairSource.replaceAll('\n', '\r\n'));
    fs.writeFileSync(path.join(direct, 'Handler.cs'), '// attachment\n');
    for (const name of ['refused', 'command-guards', 'watcher-guards', 'post-dispatch', 'clean-unknown', 'root-replacement', 'root-replacement-next', ...associatedUntitledTargets.flatMap(relative => ['before-discovery', 'after-review'].map(timing => `untitled-${relative.replaceAll('/', '-')}-${timing}`))]) {
        const model = path.join(root, name);
        fs.mkdirSync(path.join(model, 'nested'), { recursive: true });
        fs.writeFileSync(path.join(model, 'application.play'), name === 'refused' ? refusedEventSource : repairSource);
        fs.writeFileSync(path.join(model, 'Handler.cs'), '// attachment\n');
        fs.writeFileSync(path.join(model, 'nested', 'watcher-existing.txt'), 'baseline');
        if (name.startsWith('untitled-') || name === 'post-dispatch' || name === 'clean-unknown') {
            // VS Code only permits path-associated untitled documents for targets
            // that do not exist. Prepare their parent BEFORE the product watches;
            // never remove the real attachment or identity/review preimages.
            fs.mkdirSync(path.join(model, '.screenplay'));
            for (const relative of associatedUntitledTargets) {
                if (fs.existsSync(path.join(model, relative))) throw new Error(`Associated untitled target already exists: ${relative}`);
            }
        }
    }
}
