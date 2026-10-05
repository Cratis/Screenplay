// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SavedVersions } from './SavedVersions';

export interface RepairEnvironment {
    check(): SavedVersions; // Synchronous trust/configuration/root/all-buffer checks at actual dispatch.
    checkRead?(): void; // Inspection may preserve dirty buffers; never authorizes a write.
}
