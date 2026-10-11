// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { dependencyKinds } from '@cratis/screenplay-compiler';
import { DependencyMapLevel } from './DependencyMapLevel';
import type { DependencyMapOptions } from './DependencyMapOptions';
import { dependencyKindLabels } from './dependencyKindLabels';

// An entry of the View menu, in the shape the board's menu takes.
export interface DependencyMapMenuItem {
    readonly id: string;
    readonly label?: string;
    readonly checked?: boolean;
    readonly keepOpen?: boolean;
    readonly isHeader?: boolean;
    readonly isSeparator?: boolean;
    readonly onClick?: () => void;
}

// The map's settings as the entries of its View menu: the level the edges run between, then each kind of
// dependency to show. The kinds keep the menu open so several can be switched in one go.
export const dependencyMapMenuItems = (options: DependencyMapOptions, onChange: (options: DependencyMapOptions) => void): DependencyMapMenuItem[] => [
    { id: 'level-header', isHeader: true, label: 'Edges between' },
    { id: 'level-module', label: 'Modules', checked: options.level === DependencyMapLevel.Module, onClick: () => onChange({ ...options, level: DependencyMapLevel.Module }) },
    { id: 'level-feature', label: 'Features', checked: options.level === DependencyMapLevel.Feature, onClick: () => onChange({ ...options, level: DependencyMapLevel.Feature }) },
    { id: 'kinds-separator', isSeparator: true },
    { id: 'kinds-header', isHeader: true, label: 'Dependency kinds' },
    ...dependencyKinds.map(kind => {
        const shown = options.kinds.includes(kind);
        return {
            id: `kind-${kind}`, label: dependencyKindLabels[kind], checked: shown, keepOpen: true,
            onClick: () => onChange({ ...options, kinds: shown ? options.kinds.filter(candidate => candidate !== kind) : [...options.kinds, kind] }),
        };
    }),
];
