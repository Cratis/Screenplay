// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// String values follow the C# graph's reference-role contract, like the compiler's syntax kind unions.
export const dependencyKinds = ['usesFactsFrom', 'reactsTo', 'decidesFrom', 'asks', 'shows', 'verifiedWith', 'outsideTheModel'] as const;
export type DependencyKind = typeof dependencyKinds[number];
