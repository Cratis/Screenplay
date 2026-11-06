// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax } from './Authorization';
import { PropertySyntax, TypeRefSyntax } from './Declarations';
import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { EverySyntax, MappingSyntax } from './Projections';
import { QueryParameterSyntax } from './Queries';
import { SourceOptions } from './SourceOptions';
import { SyntaxNode } from './SyntaxNode';

// A 'node <Label>' declaration of a graph: a label with typed properties, exactly one of them the key.
export interface GraphNodeSyntax extends SyntaxNode {
    readonly kind: 'GraphNodeSyntax';
    readonly label: string;
    readonly properties: readonly PropertySyntax[];
}

// An 'edge <TYPE> source <Label> target <Label>' declaration of a graph.
export interface GraphEdgeSyntax extends SyntaxNode {
    readonly kind: 'GraphEdgeSyntax';
    readonly type: string;
    readonly source: string;
    readonly target: string;
    readonly identifiedBy?: string | null;
    readonly properties: readonly PropertySyntax[];
}

// A 'graph <Name>' declaration: the shape of a property graph.
export interface GraphSyntax extends SyntaxNode {
    readonly kind: 'GraphSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly documentation?: string | null;
    readonly nodes: readonly GraphNodeSyntax[];
    readonly edges: readonly GraphEdgeSyntax[];
}

// 'from <Event> [foreach <collection>] key <expr>' in a node block, or 'from <Event> source <expr> target <expr> [identified by <expr>]' in an edge block.
export interface GraphFromSyntax extends SyntaxNode {
    readonly kind: 'GraphFromSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
    readonly source?: ExpressionSyntax | null;
    readonly target?: ExpressionSyntax | null;
    readonly identity?: ExpressionSyntax | null;
    readonly forEach?: string | null;
    readonly mappings: readonly MappingSyntax[];
}

// 'remove with <Event> ...' in a graph node or edge block.
export interface GraphRemoveSyntax extends SyntaxNode {
    readonly kind: 'GraphRemoveSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
    readonly source?: ExpressionSyntax | null;
    readonly target?: ExpressionSyntax | null;
    readonly identity?: ExpressionSyntax | null;
    readonly forEach?: string | null;
    readonly detach: boolean;
}

export type GraphHandlerSyntax = GraphFromSyntax | GraphRemoveSyntax | EverySyntax;

export interface GraphNodeBlockSyntax extends SyntaxNode {
    readonly kind: 'GraphNodeBlockSyntax';
    readonly label: string;
    readonly blocks: readonly GraphHandlerSyntax[];
}

export interface GraphEdgeBlockSyntax extends SyntaxNode {
    readonly kind: 'GraphEdgeBlockSyntax';
    readonly type: string;
    readonly blocks: readonly GraphHandlerSyntax[];
}

// 'projection <Name> => graph <Graph>': events projected onto a property graph.
export interface GraphProjectionSyntax extends SyntaxNode {
    readonly kind: 'GraphProjectionSyntax';
    readonly sourceOptions?: SourceOptions;
    readonly name: string;
    readonly graph: string;
    readonly description?: string | null;
    readonly blocks: readonly (GraphNodeBlockSyntax | GraphEdgeBlockSyntax)[];
}

export interface SpecificationGraphNodeSyntax extends SyntaxNode {
    readonly kind: 'SpecificationGraphNodeSyntax';
    readonly label: string;
    readonly key: ExpressionSyntax;
    readonly absent: boolean;
    readonly properties: readonly PropertyMappingSyntax[];
}

export interface SpecificationGraphEdgeSyntax extends SyntaxNode {
    readonly kind: 'SpecificationGraphEdgeSyntax';
    readonly type: string;
    readonly source: ExpressionSyntax;
    readonly target: ExpressionSyntax;
    readonly identity?: ExpressionSyntax | null;
    readonly absent: boolean;
    readonly properties: readonly PropertyMappingSyntax[];
}

export type SpecificationGraphElementSyntax = SpecificationGraphNodeSyntax | SpecificationGraphEdgeSyntax;

// 'given graph <Graph>' or 'then graph <Graph>' with the nodes and edges it states.
export interface SpecificationGraphSyntax extends SyntaxNode {
    readonly kind: 'SpecificationGraphSyntax';
    readonly graph: string;
    readonly elements: readonly SpecificationGraphElementSyntax[];
}

// A node in a path pattern: 'alias:Label' or 'Label'.
export interface GraphPatternNodeSyntax extends SyntaxNode {
    readonly kind: 'GraphPatternNodeSyntax';
    readonly alias: string | null;
    readonly label: string;
}

// An edge and the node it leads to in a path pattern: '-alias:TYPE-> node' (outgoing) or '<-alias:TYPE- node'.
export interface GraphPatternHopSyntax extends SyntaxNode {
    readonly kind: 'GraphPatternHopSyntax';
    readonly alias: string | null;
    readonly type: string;
    readonly outgoing: boolean;
    readonly node: GraphPatternNodeSyntax;
}

// One 'match' or 'optional match' path pattern of a graph query.
export interface GraphMatchSyntax extends SyntaxNode {
    readonly kind: 'GraphMatchSyntax';
    readonly isOptional: boolean;
    readonly start: GraphPatternNodeSyntax;
    readonly hops: readonly GraphPatternHopSyntax[];
}

// '<alias>.<property> = $argument | literal' in a graph query 'where'.
export interface GraphConditionSyntax extends SyntaxNode {
    readonly kind: 'GraphConditionSyntax';
    readonly alias: string;
    readonly property: string;
    readonly value: ExpressionSyntax;
}

// '<property> = <alias>.<property>' in a graph query 'return'.
export interface GraphReturnSyntax extends SyntaxNode {
    readonly kind: 'GraphReturnSyntax';
    readonly property: string;
    readonly alias: string;
    readonly source: string;
}

// 'query <Name> => [observable] <ReadModel>[] graph <Graph>': a query that reads a graph and returns a read model.
export interface GraphQuerySyntax extends SyntaxNode {
    readonly kind: 'GraphQuerySyntax';
    readonly name: string;
    readonly returnType: TypeRefSyntax;
    readonly graph: string;
    readonly by: QueryParameterSyntax | null;
    readonly filters: readonly QueryParameterSyntax[];
    readonly authorize: AuthorizeSyntax | null;
    readonly matches: readonly GraphMatchSyntax[];
    readonly conditions: readonly GraphConditionSyntax[];
    readonly returns: readonly GraphReturnSyntax[];
    readonly isObservable: boolean;
    readonly description: string | null;
}
