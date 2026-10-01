// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ActorDocument, ActorType } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';

// The board draws an automation's or a translation's cogwheel in the row of a system role, so the system is
// there whenever the model has a slice it acts in.
export const systemActor: ActorDocument = {
    id: guidFor('actor:System'),
    name: 'System',
    actorType: ActorType.systemRole,
    description: 'The application itself, reacting to what happens and translating what other systems hold',
};
