// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax } from './Authorization';
import { TypeRefSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

export interface QueryParameterSyntax extends SyntaxNode {
    readonly kind: 'QueryParameterSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax;
}

export interface QuerySyntax extends SyntaxNode {
    readonly kind: 'QuerySyntax';
    readonly name: string;
    readonly returnType: TypeRefSyntax;
    readonly by: QueryParameterSyntax | null;
    readonly filters: readonly QueryParameterSyntax[];
    readonly description: string | null;
    readonly isObservable: boolean;
    readonly scope: string | null;
    readonly authorize: AuthorizeSyntax | null;
}
