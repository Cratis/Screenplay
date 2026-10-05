// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from '../Syntax/Commands';

// Reads remain outside the narrowed TypeScript wire contract. Retain source evidence for
// operation mapping checks without guessing that an unavailable state shape is a command field.
export const commandReadSources = new WeakMap<CommandSyntax, readonly { readModel: string; alias: string | null }[]>();
