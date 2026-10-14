// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationSupportBudgetExceeded } from './GenerationSupportBudgetExceeded';

export class GenerationWorkBudget {
    static readonly positionLimit = 2048;
    static readonly stateLimit = 10000;
    static readonly operationLimit = 100000000;
    used = 0;
    constructor(private readonly limit = GenerationWorkBudget.operationLimit) {}
    spend(operations = 1): void {
        if (!Number.isSafeInteger(operations) || operations < 0 || operations > this.limit - this.used) throw new GenerationSupportBudgetExceeded(`operation budget (${this.limit} operations)`);
        this.used += operations;
    }
}
