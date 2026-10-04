// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairLocation } from './RepairLocation';

export interface ServerDiagnostic { code: string; message: string; severity: string; location: RepairLocation }
