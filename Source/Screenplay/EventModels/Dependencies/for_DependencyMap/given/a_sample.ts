// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { compileEventModelApplication } from '../../../Mapping/compileEventModelApplication';

export const samplesDirectory = resolve(import.meta.dirname, '../../../../../..', 'Samples');

export function sampleApplication(name: string) {
    const directory = join(samplesDirectory, name);
    const sources = readdirSync(directory, { recursive: true, encoding: 'utf8' }).filter(path => path.endsWith('.play')).sort()
        .map(path => ({ path, source: readFileSync(join(directory, path), 'utf8') }));
    return compileEventModelApplication(sources).value;
}
