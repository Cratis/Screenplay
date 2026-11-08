// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// What a sink stores: read models or graphs.
export type SinkKind = 'ReadModels' | 'Graphs';

// 'sink <Name> for readmodels|graphs' - a logical place read models or graphs are stored, which the deployment maps
// to real storage. Realization metadata, never executable; names are unique ignoring case.
export interface SinkSyntax extends SyntaxNode {
    readonly kind: 'SinkSyntax';
    readonly name: string;
    readonly stores: SinkKind;
}
