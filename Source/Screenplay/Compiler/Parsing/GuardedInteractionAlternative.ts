// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ShadowingAlternative } from './GuardedActionShadowing';
import { InteractionExecution } from './InteractionExecution';

export interface GuardedInteractionAlternative extends ShadowingAlternative {
    readonly executions: readonly InteractionExecution[];
}
