// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConstraintSyntax } from '@cratis/screenplay-compiler';

export type EventConstraint = Exclude<ConstraintSyntax, { kind: 'FileConstraintSyntax' }>;
