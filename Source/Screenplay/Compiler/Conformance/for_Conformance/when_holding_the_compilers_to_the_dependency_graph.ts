// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { DependencyGraph } from '../../Dependencies/DependencyGraph';
import { ApplicationCompilation, compileApplication } from '../../Files/PlayApplicationAssembly';

interface Vector {
    name: string;
    files: Record<string, string>;
    edges: string[];
    implied: string[];
    cycles: string[][];
    order: string[];
    excludedReferences?: number;
    unresolved?: string[];
}
const vectors = (JSON.parse(readFileSync(join(__dirname, '..', 'dependency-graph.json'), 'utf8')) as { cases: Vector[] }).cases;

for (const vector of vectors) {
    describe(`when holding the compilers to the dependency graph: ${vector.name}`, () => {
        let graph: DependencyGraph;
        let reversed: DependencyGraph;
        let compilation: ApplicationCompilation;
        let reversedCompilation: ApplicationCompilation;
        beforeEach(() => {
            const files = new Map(Object.entries(vector.files));
            const reversedFiles = new Map([...files].reverse());
            compilation = compileApplication(files, [...files.keys()]);
            reversedCompilation = compileApplication(reversedFiles, [...reversedFiles.keys()]);
            graph = DependencyGraph.for(compilation.value);
            reversed = DependencyGraph.for(reversedCompilation.value);
        });
        if (Object.keys(vector.files).length > 1) {
            it('should actually reverse document arrival order', () => {
                compilation.documents.map(document => document.path).should.deep.equal(Object.keys(vector.files));
                reversedCompilation.documents.map(document => document.path).should.deep.equal(Object.keys(vector.files).reverse());
            });
        }
        it('should match the slice edges', () => {
            graph.edges.map(edge => `${edge.consumer.address}|${edge.producer.address}|${edge.kind}|${edge.evidence.map(item => `${item.role}:${item.name}`).join(',')}`).should.deep.equal(vector.edges);
        });
        it('should match the implied feature edges', () => {
            graph.implied('feature', 'feature').map(edge => `${edge.source.address}|${edge.target.address}|${edge.sliceEdges}|${edge.references}`).should.deep.equal(vector.implied);
        });
        it('should match the feature cycles', () => {
            graph.cycles('feature').map(group => group.members.map(node => node.address)).should.deep.equal(vector.cycles);
        });
        it('should match the suggested story order', () => {
            graph.suggestedOrder().slices.map(node => node.address).should.deep.equal(vector.order);
        });
        it('should match excluded shared references', () => {
            graph.excludedReferences.should.equal(vector.excludedReferences ?? 0);
        });
        it('should match unresolved references', () => {
            graph.unresolved.map(item => `${item.consumer.address}|${item.kind}|${item.role}:${item.name}`).should.deep.equal(vector.unresolved ?? []);
        });
        it('should ignore document arrival order', () => {
            reversed.edges.should.deep.equal(graph.edges);
            reversed.implied('feature', 'feature').should.deep.equal(graph.implied('feature', 'feature'));
            reversed.cycles('feature').should.deep.equal(graph.cycles('feature'));
            reversed.suggestedOrder().should.deep.equal(graph.suggestedOrder());
            reversed.unresolved.should.deep.equal(graph.unresolved);
            reversed.excludedReferences.should.equal(graph.excludedReferences);
        });
    });
}
