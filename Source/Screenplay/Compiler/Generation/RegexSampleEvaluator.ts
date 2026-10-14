// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexSampleResult } from './RegexSampleResult';

/** A host runtime cross-check must have a real deadline; timeout never proves or refutes. */
export type RegexSampleEvaluator = (pattern: string, sample: string) => RegexSampleResult;
