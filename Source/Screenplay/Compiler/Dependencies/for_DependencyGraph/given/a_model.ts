// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyGraph } from '../../DependencyGraph';
import { parse } from '../../../ScreenplayCompiler';

export const producer = 'module M\n  feature A\n    slice StateChange Producer\n      event E\n      command C\n      readmodel R\n      query Q => R\n      screen S\n';
export const graphOf = (source: string): DependencyGraph => DependencyGraph.for(parse(source, 'model.play').value);
