// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardViewMenu, type BoardDensity, type BoardTextSize, type EventModelPresentation, type EventModelPresentationChange, type MenuDropdownItem } from '@cratis/event-models';

interface ReadabilityMenuProps {
    readonly visible: boolean;
    readonly presentation: EventModelPresentation;
    readonly onChange: EventModelPresentationChange;
}

const spacing: { value: BoardDensity; label: string }[] = [
    { value: 'compact', label: 'Compact' },
    { value: 'comfortable', label: 'Comfortable' },
    { value: 'spacious', label: 'Spacious' },
];
const textSizes: { value: BoardTextSize; label: string }[] = [
    { value: 'small', label: 'Small' },
    { value: 'medium', label: 'Medium' },
    { value: 'large', label: 'Large' },
];

export const ReadabilityMenu = ({ visible, presentation, onChange }: ReadabilityMenuProps) => {
    const items: MenuDropdownItem[] = [
        { id: 'spacing-header', label: 'Spacing', isHeader: true },
        ...spacing.map(({ value, label }) => ({
            id: `spacing-${value}`, label, checked: presentation.density === value,
            onClick: () => onChange({ ...presentation, density: value }),
        })),
        { id: 'text-size-separator', isSeparator: true },
        { id: 'text-size-header', label: 'Text size', isHeader: true },
        ...textSizes.map(({ value, label }) => ({
            id: `text-size-${value}`, label, checked: presentation.textSize === value,
            onClick: () => onChange({ ...presentation, textSize: value }),
        })),
    ];
    return visible ? <BoardViewMenu title='Spacing and text size' items={items} /> : null;
};
