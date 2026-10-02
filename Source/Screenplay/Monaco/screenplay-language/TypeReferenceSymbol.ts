// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface TypeReferenceSymbol {
    readonly name: string;
    readonly isCollection: boolean;
    readonly isOptional: boolean;
}

export function typeReferenceSymbol(text: string): TypeReferenceSymbol {
    const match = /^([\w.]+)(\[\])?(\?|\s+optional)?$/.exec(text);
    return { name: match?.[1] ?? text, isCollection: match?.[2] !== undefined, isOptional: match?.[3] !== undefined };
}

export function typeReferenceText(type: TypeReferenceSymbol): string {
    return `${type.name}${type.isCollection ? '[]' : ''}${type.isOptional ? ' optional' : ''}`;
}
