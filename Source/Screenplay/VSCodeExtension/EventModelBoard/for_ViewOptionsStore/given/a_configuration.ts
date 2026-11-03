// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { WorkspaceConfiguration } from 'vscode';

export function a_configuration(initial: Record<string, unknown> = {}) {
    const values = new Map(Object.entries(initial));
    const updates: { section: string; value: unknown; target: unknown }[] = [];
    const configuration: Pick<WorkspaceConfiguration, 'get' | 'update'> = {
        get: <T>(section: string, fallback?: T) => (values.has(section) ? values.get(section) : fallback) as T,
        update: async (section, value, target) => {
            updates.push({ section, value, target });
            values.set(section, value);
        },
    };
    return { ...configuration, values, updates };
}
