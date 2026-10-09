// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SpecificationSyntax } from './Specifications';

export interface SpecificationCaseOrigin {
    readonly table: string;
    readonly case: string;
}

export const specificationCaseOrigins = new WeakMap<SpecificationSyntax, SpecificationCaseOrigin>();
