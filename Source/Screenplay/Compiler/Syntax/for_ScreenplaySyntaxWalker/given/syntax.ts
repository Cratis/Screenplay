// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonConditionSyntax } from '../../Conditions';
import { LiteralExpressionSyntax, PropertyMappingSyntax } from '../../Expressions';
import { ScreenComponentSyntax, ScreenNavigateSyntax, ScreenTitleSyntax, ToolbarItemSyntax, UiBindingSyntax } from '../../Screens';

export const location = { line: 1, column: 1 };

export function literal(value: string): LiteralExpressionSyntax {
    return { kind: 'LiteralExpressionSyntax', value, location };
}

export function binding(path: string): UiBindingSyntax {
    return { kind: 'UiBindingSyntax', bindingKind: 'DataContext', path, query: null, componentId: null,
        componentPropertyPath: null, mode: null, nullBehavior: null, expectedValueType: null, literal: null, rawText: null, location };
}

export function navigation(screen: string): ScreenNavigateSyntax {
    return { kind: 'ScreenNavigateSyntax', screen, by: null, route: null, parameters: [], location };
}

export function title(text: string): ScreenTitleSyntax {
    return { kind: 'ScreenTitleSyntax', text, location };
}

export function component(): ScreenComponentSyntax {
    return { kind: 'ScreenComponentSyntax', component: 'Card', name: 'card', stableId: null, context: null,
        properties: [], exposes: [], presentation: [], icon: null, slots: [], location };
}

export function toolbarItem(name: string): ToolbarItemSyntax {
    return { kind: 'ToolbarItemSyntax', name, syntaxKind: 'Navigate', target: 'Details', label: null, icon: null,
        parameters: [], presentation: [], location };
}

export function condition(value: string): ComparisonConditionSyntax {
    return { kind: 'ComparisonConditionSyntax', left: 'status', operator: 'Equal', right: literal(value), location };
}

export function mapping(property: string): PropertyMappingSyntax {
    return { kind: 'PropertyMappingSyntax', property, source: literal(property), location };
}
