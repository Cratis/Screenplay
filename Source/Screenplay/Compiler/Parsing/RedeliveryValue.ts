// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExactNumber } from '../Syntax/ExactNumber';

export type RedeliveryValue = string | number | boolean | null | ExactNumber | { readonly canonical: string };
