// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DesignAssignmentSyntax, DesignComponentSyntax, DesignSystemSyntax, ThemeSyntax } from '../Syntax/DesignSystemSyntax';

const paletteCategory = 'palette';

export interface ResolvedDesignVariant {
    readonly name: string;
    readonly parts: ReadonlyMap<string, string>;
    readonly states: ReadonlyMap<string, ReadonlyMap<string, string>>;
}

export interface ResolvedDesignComponent {
    readonly name: string;
    readonly variants: readonly ResolvedDesignVariant[];
}

export interface DesignProblem {
    readonly code: string;
    readonly message: string;
    readonly location: SourceLocation;
}

const sameLocation = (left: SourceLocation, right: SourceLocation): boolean => left.path === right.path && left.line === right.line && left.column === right.column;

const firstByName = (entries: readonly DesignAssignmentSyntax[]): Map<string, DesignAssignmentSyntax> => {
    const byName = new Map<string, DesignAssignmentSyntax>();
    for (const entry of entries) if (!byName.has(entry.name)) byName.set(entry.name, entry);
    return byName;
};

// Resolves one theme of a design system in one mode: every token address the theme gives a value (a literal, a palette entry or
// another token) down to its literal, and every component part through the tokens. Where a reference cannot be followed it records
// a problem instead of a value. The port of the C# DesignThemeEvaluation.
export class DesignThemeEvaluation {
    readonly tokens = new Map<string, string>();
    readonly problems: DesignProblem[] = [];
    readonly #palette: Map<string, DesignAssignmentSyntax>;
    readonly #assignments: Map<string, DesignAssignmentSyntax>;
    readonly #declared: Set<string>;
    readonly #resolved = new Map<string, string | null>();

    constructor(readonly designSystem: DesignSystemSyntax, readonly theme: ThemeSyntax, readonly mode: string | null) {
        this.#palette = firstByName(theme.palette ?? []);
        this.#assignments = firstByName(theme.values ?? []);
        const modeBlock = mode === null ? undefined : (theme.modeValues ?? []).find(block => block.mode === mode);
        for (const entry of modeBlock?.values ?? []) this.#assignments.set(entry.name, entry);
        this.#declared = new Set(designSystem.tokens.flatMap(category => category.tokens.map(token => `${category.category}.${token.name}`)));
    }

    hasValue(address: string): boolean {
        return this.#assignments.has(address);
    }

    evaluate(): readonly ResolvedDesignComponent[] {
        for (const [address, assignment] of this.#assignments) {
            if (!this.#declared.has(address)) continue;
            const value = this.#resolveToken(address, assignment, []);
            if (value !== null) this.tokens.set(address, value);
        }
        return this.designSystem.components.map(component => this.#resolveComponent(component));
    }

    #resolveComponent(component: DesignComponentSyntax): ResolvedDesignComponent {
        return {
            name: component.name,
            variants: component.variants.map(variant => {
                const parts = this.#resolveParts(variant.parts);
                const states = new Map<string, ReadonlyMap<string, string>>();
                for (const state of variant.states) {
                    const merged = new Map(parts);
                    for (const [part, value] of this.#resolveParts(state.parts)) merged.set(part, value);
                    states.set(state.name, merged);
                }
                return { name: variant.name, parts, states };
            }),
        };
    }

    #resolveParts(parts: readonly DesignAssignmentSyntax[]): Map<string, string> {
        const result = new Map<string, string>();
        for (const part of parts) {
            const value = this.tokens.get(part.value);
            if (value !== undefined) result.set(part.name, value);
        }
        return result;
    }

    #resolveToken(address: string, assignment: DesignAssignmentSyntax, path: string[]): string | null {
        const known = this.#resolved.get(address);
        if (known !== undefined) return known;
        if (path.includes(address)) {
            const cycle = [...path.slice(path.indexOf(address)), address].join(' -> ');
            this.#report(DiagnosticCodes.DesignReferenceCycle, `Theme '${this.theme.name}' resolves '${address}' through itself (${cycle})`, assignment.location);
            return null;
        }
        path.push(address);
        const value = assignment.isLiteral ? assignment.value : this.#resolveReference(assignment, path);
        path.pop();
        this.#resolved.set(address, value);
        return value;
    }

    #resolveReference(assignment: DesignAssignmentSyntax, path: string[]): string | null {
        const reference = assignment.value;
        if (reference.startsWith(`${paletteCategory}.`)) {
            const entry = this.#palette.get(reference.slice(paletteCategory.length + 1));
            if (entry === undefined) return this.#unresolved(assignment, `palette entry '${reference}' is not declared in theme '${this.theme.name}'`);
            return entry.isLiteral ? entry.value : this.#resolveToken(reference, entry, path);
        }
        if (!this.#declared.has(reference)) return this.#unresolved(assignment, `'${reference}' is not a token of design system '${this.designSystem.name}'`);
        const target = this.#assignments.get(reference);
        return target !== undefined
            ? this.#resolveToken(reference, target, path)
            : this.#unresolved(assignment, `token '${reference}' has no value in theme '${this.theme.name}'${this.mode === null ? '' : ` in mode '${this.mode}'`}`);
    }

    #unresolved(assignment: DesignAssignmentSyntax, reason: string): null {
        this.#report(DiagnosticCodes.UnresolvedDesignReference, `'${assignment.name} = ${assignment.value}' cannot be resolved: ${reason}`, assignment.location);
        return null;
    }

    #report(code: string, message: string, location: SourceLocation): void {
        if (!this.problems.some(problem => problem.code === code && sameLocation(problem.location, location))) this.problems.push({ code, message, location });
    }
}
