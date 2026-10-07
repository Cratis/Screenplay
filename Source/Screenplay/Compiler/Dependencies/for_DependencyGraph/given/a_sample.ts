// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { compileApplication } from '../../../Files/PlayApplicationAssembly';
import { DependencyGraph } from '../../DependencyGraph';

export function sampleGraph(name: string): DependencyGraph {
    const directory = resolve(__dirname, '../../../../../..', 'Samples', name);
    const files = new Map(readdirSync(directory, { recursive: true, encoding: 'utf8' }).filter(path => path.endsWith('.play')).sort().map(path => [path, readFileSync(join(directory, path), 'utf8')]));
    return DependencyGraph.for(compileApplication(files).value);
}
