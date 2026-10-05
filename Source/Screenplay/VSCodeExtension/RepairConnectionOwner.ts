// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure } from './RepairClient';
import { RepairSession } from './RepairSession';
import { RepairRootWatch } from './RepairRootWatch';

let nextOwner = 0;

/** One connection's authority and resources, including a possibly retiring Apply. */
export class RepairConnectionOwner {
    session!: RepairSession;
    connecting?: Promise<RepairSession>;
    failure?: RepairFailure;
    rootWatch?: RepairRootWatch;
    retired = false;
    applyPending = false;
    readonly resources: { dispose(): void }[] = [];
    readonly id = ++nextOwner;
    constructor(readonly generation: number) {}
    disposeWatchers(): void { for (const resource of this.resources.splice(0)) resource.dispose(); }
    retire(cause = 'owner-retire'): void {
        if (this.retired) return;
        this.session?.trace(`owner-retire:${cause}`);
        this.retired = true;
        this.disposeWatchers();
        if (!this.session) return; // Construction can refuse before a process exists.
        this.session.invalidate(cause);
        // Renames and configuration changes must not kill an in-flight Apply request.
        // A settled (even uncertain) Apply holds no process need; the recovery barrier lives with the command layer.
        if (!this.session.applyInFlight) this.session.dispose(cause);
    }
    assertCurrent(current: RepairConnectionOwner | undefined, generation: number): void {
        if (this.retired || current !== this || generation !== this.generation) {
            this.session?.trace('guard:owner', { owns: current === this, retired: this.retired, currentGeneration: generation });
            throw new RepairFailure('StaleEpoch', 'Root or configuration changed. Rediscover and review a fresh repair.');
        }
        if (this.failure) throw this.failure;
    }
}
