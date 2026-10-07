// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { DeclaredDependencies } from '../../Dependencies/DeclaredDependencies';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { CompilationResult, parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

interface Vector {
    name: string;
    files: Record<string, string>;
    diagnostics: string[];
    report: string[];
    document?: boolean;
    sample?: string;
    insertions?: { file: string; header: string; targets: string[] }[];
    uncovered?: string[];
    evidence?: string[];
}
const vectors = (JSON.parse(readFileSync(join(__dirname, '..', 'declared-dependencies.json'), 'utf8')) as { cases: Vector[] }).cases;
for (const vector of vectors) {
    describe(`when holding the compilers to declared dependencies: ${vector.name}`, () => {
        let compilation: CompilationResult<ApplicationSyntax>;
        beforeEach(() => {
            const directory = resolve(__dirname, '../../../../..', 'Samples', vector.sample ?? '');
            const files = vector.sample === undefined ? new Map(Object.entries(vector.files)) : new Map(readdirSync(directory, { recursive: true, encoding: 'utf8' }).filter(path => path.endsWith('.play')).sort().map(path => [path, readFileSync(join(directory, path), 'utf8')]));
            for (const insertion of vector.insertions ?? []) {
                const indent = insertion.header.length - insertion.header.trimStart().length + 2;
                files.set(insertion.file, files.get(insertion.file)!.replace(insertion.header + '\n', insertion.header + '\n' + insertion.targets.map(target => ' '.repeat(indent) + 'depends on ' + target + '\n').join('')));
            }
            compilation = vector.document ? parse(files.get('application.play')!, 'application.play') : compileApplication(files, [...files.keys()]);
        });
        it('should match declared dependency findings and locations', () => {
            compilation.diagnostics.filter(item => ['PLAY0022', 'PLAY0198', 'PLAY0552', 'PLAY0553', 'PLAY0554', 'PLAY0555', 'PLAY0556'].includes(item.code))
                .map(item => `${item.code}@${item.location.path}:${item.location.line}`).should.deep.equal(vector.diagnostics);
        });
        it('should match declaration and per-evidence coverage statuses', () => {
            const report = DeclaredDependencies.for(compilation.value);
            report.containers.flatMap(item => [
                ...item.declarations.map(declaration => `${item.container.address}|${declaration.syntax.target}|${declaration.resolved ?? ''}|${declaration.status}`),
                ...(vector.sample === undefined ? item.edges : []).map(edge => `${item.container.address}|${edge.evidence.consumer.address}|${edge.evidence.producer.address}|${edge.evidence.kind}|${edge.status}|${edge.coveringDeclarations.map(declaration => declaration.target).join(',')}`),
            ]).should.deep.equal(vector.report);
        });
        if (vector.uncovered !== undefined) {
            it('should name each uncovered producer and retain source evidence', () => {
                const findings = DeclaredDependencies.for(compilation.value).diagnostics.filter(item => item.code === 'PLAY0552');
                findings.map(item => item.message.match(/depends on '(.+)' without declaring/)![1]).should.deep.equal(vector.uncovered);
                for (const evidence of vector.evidence ?? []) findings.map(item => item.message).join('; ').should.contain(evidence);
            });
        }
    });
}
