// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The fixed vocabulary of a design system: what a token category is called, which parts a component may style, which states
// it may have, and which token categories a part accepts. The port of the C# DesignVocabulary.
const partCategories: Readonly<Record<string, readonly string[]>> = {
    text: ['color'],
    background: ['color'],
    border: ['color', 'border'],
    radius: ['radius'],
    height: ['size', 'space'],
    padding: ['space', 'size'],
    shadow: ['shadow'],
    font: ['font', 'type'],
};

export const designCategories: readonly string[] = ['color', 'space', 'size', 'radius', 'border', 'shadow', 'font', 'type', 'motion', 'opacity', 'z'];
export const designParts: readonly string[] = ['text', 'background', 'border', 'radius', 'height', 'padding', 'shadow', 'font'];
export const designStates: readonly string[] = ['default', 'hover', 'focus', 'active', 'disabled', 'selected'];

// The token categories a part takes a value from; none for a part this vocabulary does not know.
export const categoriesForPart = (part: string): readonly string[] => Object.hasOwn(partCategories, part) ? partCategories[part] : [];
