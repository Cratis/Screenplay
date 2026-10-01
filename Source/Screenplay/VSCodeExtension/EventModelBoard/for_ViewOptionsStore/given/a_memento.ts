// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Memento } from 'vscode';

// VS Code's storage for the user, held in memory.
export function a_memento(initial: Record<string, unknown> = {}): Memento & { readonly values: Map<string, unknown> } {
    const values = new Map(Object.entries(initial));
    return {
        values,
        keys: () => [...values.keys()],
        get: <T>(key: string, fallback?: T) => (values.has(key) ? values.get(key) as T : fallback) as T,
        update: (key: string, value: unknown) => {
            values.set(key, value);
            return Promise.resolve();
        },
    };
}
