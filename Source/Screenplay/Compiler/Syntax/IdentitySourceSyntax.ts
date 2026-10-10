// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClaimIdentitySourceSyntax } from './ClaimIdentitySourceSyntax';
import { CodeIdentitySourceSyntax } from './CodeIdentitySourceSyntax';
import { FileIdentitySourceSyntax } from './FileIdentitySourceSyntax';
import { QueryIdentitySourceSyntax } from './QueryIdentitySourceSyntax';

export type IdentitySourceSyntax = ClaimIdentitySourceSyntax | QueryIdentitySourceSyntax | CodeIdentitySourceSyntax | FileIdentitySourceSyntax;
