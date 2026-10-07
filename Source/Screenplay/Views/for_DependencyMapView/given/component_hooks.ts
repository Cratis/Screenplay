// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactElement, ReactNode } from 'react';

/** Exercise component event handlers without installing a browser or changing the host's dependencies. */
export const hooks = {
    values: [] as unknown[],
    cursor: 0,
    reset() { this.values = []; this.cursor = 0; },
    render(component: () => ReactNode) { this.cursor = 0; return elementsIn(component()); },
};

export const componentHooks = {
    useId: () => 'map-marker',
    useMemo: <Value>(factory: () => Value) => factory(),
    useEffect: () => undefined,
    useRef: <Value>(initial: Value) => ({ current: initial }),
    useState: <Value>(initial?: Value | (() => Value)) => {
        const index = hooks.cursor++;
        if (index >= hooks.values.length) hooks.values[index] = typeof initial === 'function' ? (initial as () => Value)() : initial;
        return [hooks.values[index] as Value, (value: Value | ((previous: Value) => Value)) => {
            hooks.values[index] = typeof value === 'function' ? (value as (previous: Value) => Value)(hooks.values[index] as Value) : value;
        }] as const;
    },
};

function elementsIn(node: ReactNode): ReactElement<Record<string, unknown>>[] {
    if (Array.isArray(node)) return node.flatMap(elementsIn);
    if (!node || typeof node !== 'object' || !('props' in node) || !('type' in node)) return [];
    const element = node as ReactElement<Record<string, unknown>>;
    return [element, ...Object.values(element.props).flatMap(value => elementsIn(value as ReactNode))];
}

export function invoke(element: ReactElement<Record<string, unknown>>, handler: string, argument?: unknown) {
    const callback = element.props[handler];
    if (typeof callback !== 'function') throw new Error(`Missing ${handler} handler`);
    callback(argument);
}
