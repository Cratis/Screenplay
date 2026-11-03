// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The board defaults absent members, so omitting blank prose keeps existing documents byte-identical.
export function proseOf(node: { description?: string | null; documentation?: string | null } | undefined): { description?: string; documentation?: string } {
    return {
        ...(node?.description?.trim() ? { description: node.description } : {}),
        ...(node?.documentation?.trim() ? { documentation: node.documentation } : {}),
    };
}
