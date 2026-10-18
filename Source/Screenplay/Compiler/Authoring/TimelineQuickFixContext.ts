// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationCompilation, compileApplication } from '../Files/PlayApplicationAssembly';
import { AuthoredOrderStep, authoredOrderOf, authoredOriginsOf, sameAuthoredStep } from '../Files/AuthoredOrder';
import { TimelineFinding, timelineOrderFindings } from '../Files/TimelineOrder';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';
import { splitLines } from '../Parsing/SourceLineSplitter';
import { QuickFixCandidate } from './QuickFixCandidate';
import { applyQuickFixEdits } from './QuickFixes';

export interface TimelineEntry {
    readonly node: SyntaxNode;
    readonly parent: ApplicationSyntax | ModuleSyntax | FeatureSyntax;
    readonly member: 'modules' | 'features' | 'slices' | 'fileImports';
    readonly index: number;
}

export class TimelineQuickFixContext {
    readonly ranks: ReadonlyMap<string, number>;
    readonly origins: ReadonlyMap<string, readonly AuthoredOrderStep[]>;
    readonly findings: readonly TimelineFinding[];
    readonly #parsed = new Map<string, ReturnType<typeof parseForAuthoring>>();
    constructor(readonly documents: ReadonlyMap<string, string>, readonly compiled: ApplicationCompilation) {
        this.ranks = authoredOrderOf(compiled.value);
        this.origins = authoredOriginsOf(compiled.value);
        this.findings = timelineOrderFindings(compiled.value);
    }
    block(step: AuthoredOrderStep, occurrence = false): string[] {
        return [...this.ranks].sort((left, right) => left[1] - right[1]).filter(([key]) => this.origins.get(key)!.some(origin => occurrence
            ? origin.path === step.path && origin.node.kind === step.node.kind && origin.node.location.line === step.node.location.line && origin.node.location.column === step.node.location.column
            : sameAuthoredStep(origin, step))).map(([key]) => key);
    }
    move(moved: readonly string[], anchor: readonly string[], after: boolean): ReadonlyMap<string, number> | undefined {
        const keys = [...this.ranks].sort((left, right) => left[1] - right[1]).map(([key]) => key);
        const contiguous = (block: readonly string[]) => block.length > 0 && block.every((key, index) => keys[keys.indexOf(block[0]) + index] === key);
        if (!contiguous(moved) || !contiguous(anchor) || moved.some(key => anchor.includes(key))) return undefined;
        const result = keys.filter(key => !moved.includes(key));
        result.splice(result.indexOf(after ? anchor.at(-1)! : anchor[0]) + (after ? 1 : 0), 0, ...moved);
        return new Map(result.map((key, index) => [key, index]));
    }
    safe(ranks: ReadonlyMap<string, number>, target: TimelineFinding): boolean {
        const allowed = new Set(this.findings.filter(finding => finding.key !== target.key).map(finding => finding.key));
        return timelineOrderFindings(this.compiled.value, ranks).every(finding => allowed.has(finding.key));
    }
    parsed(path: string): ReturnType<typeof parseForAuthoring> {
        if (!this.#parsed.has(path)) this.#parsed.set(path, parseForAuthoring(this.documents.get(path)!, path, this.compiled.documents.find(document => document.path === path)!.placement, false));
        return this.#parsed.get(path)!;
    }
    entries(path: string): TimelineEntry[] {
        const result: TimelineEntry[] = [];
        const visit = (parent: ApplicationSyntax | ModuleSyntax | FeatureSyntax) => {
            for (const member of ['modules', 'features', 'slices', 'fileImports'] as const) {
                const children = (parent as unknown as Record<string, readonly SyntaxNode[]>)[member] ?? [];
                children.forEach((node, index) => {
                    result.push({ node, parent, member, index });
                    if (node.kind === 'ModuleSyntax' || node.kind === 'FeatureSyntax') visit(node as ModuleSyntax | FeatureSyntax);
                });
            }
        };
        visit(this.parsed(path).value);
        return result;
    }
    entry(step: AuthoredOrderStep, entries: readonly TimelineEntry[]): TimelineEntry | undefined {
        const found = entries.filter(entry => entry.node.kind === step.node.kind && entry.node.location.line === step.node.location.line && entry.node.location.column === step.node.location.column);
        return found.length === 1 ? found[0] : undefined;
    }
    verify(candidate: QuickFixCandidate, path: string, ranks: ReadonlyMap<string, number>, finding: TimelineFinding): boolean {
        const source = applyQuickFixEdits(this.documents.get(path)!, candidate.fix.edits);
        if (source === undefined) return false;
        const original = this.parsed(path);
        const parsed = parseForAuthoring(source, path, this.compiled.documents.find(document => document.path === path)!.placement, false);
        const shape = (value: unknown, replace = false) => JSON.stringify(value, (key, node: unknown) => ['location', 'usesLocation', 'targetLocation'].includes(key) ? undefined : replace && candidate.change !== undefined && node === candidate.change.node ? candidate.change.replacement : node);
        if (shape([original.value, original.triggerData], true) !== shape([parsed.value, parsed.triggerData])) return false;
        const documents = new Map(this.documents).set(path, source);
        const compiled = compileApplication(documents);
        if (compiled.success !== this.compiled.success || compiled.documents.length !== this.compiled.documents.length || compiled.documents.some(document => {
            const before = this.compiled.documents.find(original => original.path === document.path);
            return before === undefined || before.isPlacementResolved !== document.isPlacementResolved || JSON.stringify(before.placement) !== JSON.stringify(document.placement);
        })) return false;
        const actual = authoredOrderOf(compiled.value);
        if (actual.size !== ranks.size || [...ranks].some(([key, rank]) => actual.get(key) !== rank)) return false;
        const problems = (application: ApplicationCompilation) => application.diagnostics.filter(diagnostic => diagnostic.severity !== 'information').map(diagnostic => JSON.stringify([diagnostic.code, diagnostic.severity, diagnostic.message, diagnostic.location.path]));
        const before = problems(this.compiled), after = problems(compiled);
        if (after.some(problem => after.filter(value => value === problem).length > before.filter(value => value === problem).length)) return false;
        const allowed = new Set(this.findings.filter(original => original.key !== finding.key).map(original => original.key));
        return timelineOrderFindings(compiled.value).every(candidate => allowed.has(candidate.key));
    }
}

/** Move intact physical text blocks; keep the enclosing source and its line-ending convention. */
export function moveTimelineText(source: string, moved: SyntaxNode, anchor: SyntaxNode, after: boolean): string {
    const lines = splitLines(source);
    const span = (node: SyntaxNode) => {
        const line = lines[node.location.line - 1];
        const indent = /^\s*/.exec(line.raw)![0].length;
        const next = lines.slice(node.location.line).find(candidate => candidate.raw.trim().length > 0 && !candidate.raw.trimStart().startsWith('//') && /^\s*/.exec(candidate.raw)![0].length <= indent);
        return { start: line.startOffset, end: next?.startOffset ?? source.length };
    };
    const left = span(moved), right = span(anchor);
    const target = after ? right.end : right.start;
    const ending = source.includes('\r\n') ? '\r\n' : '\n';
    let block = source.slice(left.start, left.end);
    if (target < source.length && !block.endsWith('\n')) block += ending;
    if (target === source.length && !source.endsWith('\n')) block = ending + block.replace(/\r?\n$/, '');
    return target < left.start ? source.slice(0, target) + block + source.slice(target, left.start) + source.slice(left.end)
        : source.slice(0, left.start) + source.slice(left.end, target) + block + source.slice(target);
}
