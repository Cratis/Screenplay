// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexExpression } from './RegexExpression';

export interface RegexConstructionStep { readonly expression: RegexExpression; readonly complete: boolean; readonly copies?: number; }
