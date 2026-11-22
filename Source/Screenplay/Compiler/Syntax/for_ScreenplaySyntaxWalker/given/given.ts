// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function given<TContext>(contextType: new () => TContext, specifications: (context: TContext) => void): () => void {
    return () => specifications(new contextType());
}
