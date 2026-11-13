// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pathToFileURL } from 'node:url';

function splitPatterns(split) {
    if (split == null) return [];
    const patterns = Array.isArray(split) ? split : [split];
    if (patterns.length === 0 || patterns.some(pattern => typeof pattern !== 'string' || pattern.trim() === '')) {
        throw new Error('A split requires nonempty class patterns');
    }
    return patterns;
}

export function expand(shards) {
    if (!Array.isArray(shards.include) || shards.include.length === 0) {
        throw new Error('At least one specification shard is required');
    }
    return { include: shards.include.flatMap(shard => {
        const patterns = splitPatterns(shard.split);
        const base = { name: shard.name, projects: shard.projects };
        if (patterns.length === 0) return [{ ...base, args: [] }];
        const exclusions = [];
        const jobs = patterns.map((pattern, index) => {
            const job = { ...base, name: `${shard.name}, part ${index + 1}`, args: ['-class', pattern, ...exclusions] };
            exclusions.push('-class-', pattern);
            return job;
        });
        return [...jobs, { ...base, name: `${shard.name}, part ${patterns.length + 1}`, args: exclusions }];
    }) };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    console.log(JSON.stringify(expand(JSON.parse(process.argv[2]))));
}
