// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from '../Syntax/Declarations';
import { canonicalExactText } from '../Syntax/ExactMathFacts';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { RefusalDeclarations } from './RefusalDeclarations';
import { RedeliveryValue } from './RedeliveryValue';

// Undefined means undecidable, not the authored null literal. Mirror TryValue in the C#
// specification consistency validator, including nominal enums and structured fixture values.
export function redeliveryValue(expression: ExpressionSyntax, type: TypeRefSyntax | null, declarations: RefusalDeclarations): RedeliveryValue | undefined {
    const concept = type === null || type.isCollection ? undefined : declarations.concepts.get(type.name);
    const enumeration = concept !== undefined && concept.type === 'Enum' ? concept : null;
    const member = (text: string) => enumeration !== null && text.startsWith(`${enumeration.name}.`) ? text.substring(enumeration.name.length + 1) : text;
    if (type?.isCollection && expression.kind === 'ListExpressionSyntax' && declarations.compatible({ ...type, isCollection: false }, { ...type, isCollection: false }) === true) {
        const items = expression.items.map(item => redeliveryValue(item, { ...type, isCollection: false, isOptional: false }, declarations));
        return items.some(item => item === undefined) ? undefined : { canonical: `[${items.map(canonical).join(',')}]` };
    }
    const properties = type === null ? undefined : declarations.types.get(type.name)?.properties;
    if (type !== null && !type.isCollection && expression.kind === 'ObjectExpressionSyntax' && properties !== undefined) {
        const members = expression.members.map(item => {
            const property = declarations.property(properties, item.name);
            return { name: item.name, value: property === null ? undefined : redeliveryValue(item.value, property.type, declarations) };
        });
        return members.some(item => item.value === undefined) ? undefined : { canonical: `{${members.sort((left, right) => left.name < right.name ? -1 : left.name > right.name ? 1 : 0).map(item => `${JSON.stringify(item.name)}:${canonical(item.value)}`).join(',')}}` };
    }
    if (expression.kind === 'LiteralExpressionSyntax') return enumeration !== null && typeof expression.value === 'string' ? member(expression.value) : expression.value;
    if (expression.kind === 'PathExpressionSyntax' && enumeration !== null) return member(expression.path);
    return undefined;
}

function canonical(value: RedeliveryValue | undefined): string {
    if (typeof value === 'object' && value !== null) return 'canonical' in value ? value.canonical : canonicalExactText(value);
    return JSON.stringify(value)!;
}

export function sameRedeliveryValue(left: ExpressionSyntax, right: ExpressionSyntax, type: TypeRefSyntax | null, declarations: RefusalDeclarations): boolean | null {
    const first = redeliveryValue(left, type, declarations);
    const second = redeliveryValue(right, type, declarations);
    if (first === undefined || second === undefined) return null;
    return canonical(first) === canonical(second);
}
