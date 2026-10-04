// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure } from './RepairClient';
import { RepairSession } from './RepairSession';
import { RepairRootWatch } from './RepairRootWatch';

/** One connection's authority and resources, including a possibly retiring Apply. */
export class RepairConnectionOwner {
    session!: RepairSession;
    connecting?: Promise<RepairSession>;
    failure?: RepairFailure;
    rootWatch?: RepairRootWatch;
    retired = false;
    applyPending = false;
    readonly resources: { dispose(): void }[] = [];
    constructor(readonly generation: number) {}
    disposeWatchers(): void { for (const resource of this.resources.splice(0)) resource.dispose(); }
    retire(): void {
        if (this.retired) return;
        this.retired = true;
        this.disposeWatchers();
        if (!this.session) return; // Construction can refuse before a process exists.
        this.session.invalidate();
        // Renames and configuration changes must not kill a possibly installed transaction.
        if (!this.session.applyDispatched) this.session.dispose();
    }
    assertCurrent(current: RepairConnectionOwner | undefined, generation: number): void {
        if (this.retired || current !== this || generation !== this.generation) throw new RepairFailure('StaleEpoch', 'Root or configuration changed. Rediscover and review a fresh repair.');
        if (this.failure) throw this.failure;
    }
}
