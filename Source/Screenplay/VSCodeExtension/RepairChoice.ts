// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairLocation } from './RepairLocation';

export interface RepairChoice {
    token: string;
    code: string;
    title: string;
    location: RepairLocation;
    subject: { revision: string; documentId: string; path: string };
    /** Semantic addresses a contract-changing repair retires, as the server described them. */
    retiredAddresses?: string[];
}
