// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';

/** Approve passive observation only for the synthetic root owned by this installed-client lifetime. */
export function nativeObservationRoot(model: string, caseName: string, observe: boolean): string {
    // Command guards permit late-expiry retries only with observed native-root attribution; CI needs
    // the same passive evidence without relying on SCREENPLAY_REPAIR_OBSERVE being set externally.
    if (caseName === 'dirty') return path.join(model, 'command-guards');
    return caseName === 'clean' ? path.join(model, 'clean-unknown') : observe ? path.join(model, caseName.startsWith('missed-') ? caseName : 'command-guards') : '';
}
