// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, eventDeclarations, ExpressionSyntax, FeatureSyntax, PropertyMappingSyntax, SpecificationExampleSyntax, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { CompletionEntry, specificationStepItems } from './completion-items';
import { ExampleAnalysis } from './ExampleAnalysis';
import { FixtureKind } from './FixtureKind';
import { withoutComment } from './document-context';
import { bmpWordCharacters } from './bmp-word-characters';

const exampleTypePrefix = new RegExp(`^\\s*example\\s+[A-Z][${bmpWordCharacters}]*\\s*:\\s*([${bmpWordCharacters}.]*)$`);
const fixtureStepPrefix = new RegExp(`^\\s*(given|when|then)\\s+(?:(readmodel|append)\\s+)?([${bmpWordCharacters}.]*)$`);

// Editor-only projection of parser-owned fixtures. It does not bind, execute, supply defaults,
// or publish ESM. Scoped candidates use the same nearest-shared-prefix rule as the compiler.
export function exampleAnalysis(syntax: unknown, path: string, lines: string[], range: (line: number) => number[], hiddenScopes: ReadonlySet<string>, diagnostics: readonly { severity: string; code: string }[]): ExampleAnalysis {
    const application = syntax as ApplicationSyntax;
    const declarations: { name: string; scope: readonly string[]; fixtureKind?: FixtureKind; example?: SpecificationExampleSyntax }[] = [];
    const rejected = diagnostics.some(diagnostic => diagnostic.severity === 'error')
        ? diagnostics.some(diagnostic => diagnostic.code === 'PLAY0519') ? '**Invalid duplicate assignments; no effective values selected.**' : '**Invalid source; no effective values selected.**'
        : null;
    const contexts = new Map<number, readonly string[]>();
    const specifications: { node: SpecificationSyntax; scope: readonly string[] }[] = [];
    const own = (node: { location: { path?: string; line: number } }, scope: readonly string[]) => {
        if (node.location.path === path && node.location.line > 0) for (const line of range(node.location.line - 1)) contexts.set(line, scope);
    };
    const examples = (nodes: readonly SpecificationExampleSyntax[] | undefined, scope: readonly string[]) => {
        for (const example of nodes ?? []) { declarations.push({ name: example.name, scope, example }); own(example, scope); }
    };
    examples(application.examples, []);
    declarations.push(...[...application.concepts, ...application.types].map(node => ({ name: node.name, scope: [] })));
    const features = (nodes: readonly FeatureSyntax[], parent: readonly string[]) => {
        for (const feature of nodes) {
            const scope = [...parent, feature.name];
            own(feature, scope);
            examples(feature.examples, scope);
            for (const slice of feature.slices) {
                const scope = [...parent, feature.name, slice.name];
                own(slice, scope);
                examples(slice.examples, scope);
                const events = new Map<string, ReturnType<typeof eventDeclarations>[number][]>();
                for (const event of eventDeclarations(slice)) events.set(event.name, [...events.get(event.name) ?? [], event]);
                for (const generations of events.values()) {
                    const latest = Math.max(...generations.map(event => event.generation));
                    declarations.push(...generations.filter(event => event.generation === latest).map(event => ({ name: event.name, scope, fixtureKind: FixtureKind.Event })));
                }
                declarations.push(...slice.commands.map(node => ({ name: node.name, scope, fixtureKind: FixtureKind.Command })),
                    ...slice.readModels.map(node => ({ name: node.name, scope, fixtureKind: FixtureKind.ReadModel })));
                for (const node of slice.specifications) { specifications.push({ node, scope }); own(node, scope); examples(node.examples, scope); }
            }
            features(feature.features, scope);
        }
    };
    for (const module of application.modules) {
        own(module, [module.name]);
        examples(module.examples, [module.name]);
        features(module.features, [module.name]);
    }
    const candidates = (reference: string, scope: readonly string[]) => {
        const parts = reference.split('.');
        const named = declarations.filter(node => node.name === parts.at(-1));
        const qualifiers = parts.slice(0, -1);
        if (qualifiers.length) return named.filter(node => qualifiers.length <= node.scope.length && qualifiers.every((part, index) => node.scope[node.scope.length - qualifiers.length + index] === part));
        for (let depth = scope.length; depth >= 0; depth--) {
            const visible = named.filter(node => depth <= node.scope.length && scope.slice(0, depth).every((part, index) => node.scope[index] === part));
            if (visible.length) return visible;
        }
        return [];
    };
    const unique = (reference: string, scope: readonly string[]) => {
        const matches = candidates(reference, scope);
        return matches.length === 1 ? matches[0] : undefined;
    };
    const kind = (node: typeof declarations[number]): FixtureKind | undefined => node.example ? unique(node.example.type, node.scope)?.fixtureKind : node.fixtureKind;
    const display = (node: typeof declarations[number]) => [...node.scope.filter(part => !hiddenScopes.has(part)), node.name].join('.');
    const targets = (scope: readonly string[], expected?: FixtureKind, typesOnly = false, prefix = ''): CompletionEntry[] => declarations.flatMap(node => {
        const fixtureKind = kind(node);
        if (!fixtureKind || (expected && fixtureKind !== expected) || (typesOnly && node.example)) return [];
        // A full scope-qualified spelling is the fallback for a shadowed declaration. Never
        // suggest a name that resolves to a competitor or an ambiguous declaration.
        const full = [...node.scope, node.name].join('.');
        const name = unique(node.name, scope) === node ? node.name : full;
        if (unique(name, scope) !== node) return [];
        const qualifiers = prefix.split('.').slice(0, -1).join('.');
        if (qualifiers && !full.endsWith(`${qualifiers}.${node.name}`)) return [];
        if (name === full && node.scope.some(part => hiddenScopes.has(part)) && unique(node.name, scope) !== node) return [];
        return [{ label: unique(node.name, scope) === node ? node.name : display(node), insertText: qualifiers ? node.name : name,
            documentation: node.example ? `Example of ${node.example.type}. Step assignments override example values; no implicit defaults.` : `Declared ${fixtureKind}.` }];
    });
    const effective = (example: SpecificationExampleSyntax | undefined, values: readonly PropertyMappingSyntax[], generated: readonly PropertyMappingSyntax[], destination: ExpressionSyntax | null, authoredRoute?: Pick<SpecificationExampleSyntax, 'stream' | 'noStream'>) => {
        if (rejected) return rejected;
        const merge = (base: readonly PropertyMappingSyntax[], overrides: readonly PropertyMappingSyntax[], prefix = '') => {
            if (new Set(base.map(value => value.property)).size !== base.length || new Set(overrides.map(value => value.property)).size !== overrides.length) return null;
            const result = new Map(base.map(value => [value.property, { value, origin: 'example', replaced: undefined as PropertyMappingSyntax | undefined }]));
            for (const value of overrides) {
                const replaced = result.get(value.property)?.value;
                result.delete(value.property);
                result.set(value.property, { value, origin: replaced ? 'override' : 'authored', replaced });
            }
            return [...result.values()].map(({ value, origin, replaced }) => `${prefix}${value.property} = ${expressionText(value.source)} — ${origin}${origin === 'example' ? ` ${example!.name}` : ''}${replaced ? ` (replaces ${expressionText(replaced.source)} from ${example!.name})` : ''}`);
        };
        const properties = merge(example?.values ?? [], values);
        const fixtures = merge(example?.generatedValues ?? [], generated, 'generated ');
        if (!properties || !fixtures) return '**Invalid duplicate assignments; no effective values selected.**';
        const selected = destination ?? example?.for;
        const priorRoute = example?.stream ?? example?.noStream;
        const replacementRoute = authoredRoute?.stream ?? authoredRoute?.noStream;
        const route = replacementRoute ?? priorRoute;
        const routeText = (value: NonNullable<typeof route>): string => value.kind === 'SpecificationNoStreamSyntax' ? 'no stream' :
            `stream ${value.eventSource}.${value.stream}${value.streamId ? ` streamId = ${expressionText(value.streamId.source)}` : value.streamIdParts.length ? ` streamId ${value.streamIdParts.map(part => `${part.property} = ${expressionText(part.source)}`).join(', ')}` : ''}`;
        const routing = route ? [`${routeText(route)} — ${replacementRoute ? priorRoute ? `override (replaces ${routeText(priorRoute)} from ${example!.name})` : 'authored' : `example ${example!.name}`}`] : [];
        return [...properties, ...fixtures, ...routing, ...(selected ? [`for ${expressionText(selected)} — ${destination ? example?.for ? `override (replaces ${expressionText(example.for)} from ${example.name})` : 'authored' : `example ${example!.name}`}`] : [])].join('\n');
    };
    const hovers = new Map<number, { start: number; length: number; content: string }>();
    for (const declaration of declarations.filter(node => node.example?.location.path === path)) {
        const example = declaration.example!;
        const line = example.location.line - 1;
        const prefix = withoutComment(lines[line] ?? '').match(/^\s*example\s+/)?.[0];
        if (prefix) hovers.set(line, { start: prefix.length + 1, length: example.name.length, content: `**example ${display(declaration)} : ${example.type}**\n\n${kind(declaration) ? effective(example, [], [], null) : 'Unresolved or ambiguous type; no effective values selected.'}` });
    }
    for (const { node, scope } of specifications) {
        const steps = [
            ...node.given.map(step => ({ step, name: step.eventType, expected: FixtureKind.Event, values: step.values, generated: [], destination: step.for, prefix: /^\s*given\s+/ })),
            ...node.givenReadModels.map(step => ({ step, name: step.name, expected: FixtureKind.ReadModel, values: step.properties, generated: [], destination: null, prefix: /^\s*given\s+readmodel\s+/ })),
            ...(node.when ? [{ step: node.when, name: node.when.commandType, expected: FixtureKind.Command, values: node.when.values, generated: node.when.generatedValues ?? [], destination: node.when.for, prefix: /^\s*when\s+/ }] : []),
            ...(node.whenAppended ? [{ step: node.whenAppended, name: node.whenAppended.eventType, expected: FixtureKind.Event, values: node.whenAppended.values, generated: [], destination: node.whenAppended.for, prefix: /^\s*when\s+append\s+/ }] : []),
            ...node.thenEvents.map(step => ({ step, name: step.eventType, expected: FixtureKind.Event, values: step.values, generated: [], destination: step.for, prefix: /^\s*then\s+/ })),
            ...node.thenReadModels.map(step => ({ step, name: step.name, expected: FixtureKind.ReadModel, values: step.properties, generated: [], destination: null, prefix: /^\s*then\s+readmodel\s+/ })),
        ];
        for (const { step, name, expected, values, generated, destination, prefix } of steps.filter(entry => entry.step.location.path === path)) {
            const resolved = unique(name, scope);
            const competing = candidates(name, scope).some(node => node.example);
            if (!resolved?.example && (resolved || !competing)) continue;
            const line = step.location.line - 1;
            const start = withoutComment(lines[line] ?? '').match(prefix)?.[0].length;
            if (start === undefined) continue;
            const content = !resolved ? '**Ambiguous example; no effective values selected.**' : kind(resolved) !== expected ? '**Example kind mismatch; no effective values selected.**'
                : `**${expected} ${resolved.example?.type ?? name}${resolved.example ? ` — example ${display(resolved)}` : ''}**\n\n${effective(resolved.example, values, generated, destination, 'eventType' in step ? step : undefined)}\n\nAuthored fixture values, not execution results. Matching is unchanged.`;
            hovers.set(line, { start: start + 1, length: name.length, content });
        }
    }
    return {
        eventExamples: declarations.filter(declaration => declaration.example?.location.path === path && kind(declaration) === FixtureKind.Event).map(declaration => declaration.example!),
        completions(line, before) {
            const scope = contexts.get(line) ?? [];
            const header = before.match(exampleTypePrefix);
            if (header) return targets(scope, undefined, true, header[1]);
            // A parser-rejected incomplete step still belongs to its specification's range.
            const inSpecification = specifications.some(({ node }) => node.location.path === path && range(node.location.line - 1).includes(line));
            if (!inSpecification) return null;
            const step = before.match(fixtureStepPrefix);
            if (!step) return null;
            if (['clock', 'caller', 'capture', 'trigger', 'query', 'result', 'error', 'denied', 'returns', 'no', 'operation', 'compensated'].includes(step[3])) return null;
            const expected = step[2] === 'readmodel' ? FixtureKind.ReadModel : step[2] === 'append' || step[1] !== 'when' ? FixtureKind.Event : FixtureKind.Command;
            const entries = targets(scope, expected, false, step[3]);
            // Keep the existing planner's keyword behavior when there are no fixture targets.
            if (!entries.length) return null;
            return step[2] ? entries : [...specificationStepItems[step[1] as 'given' | 'when' | 'then'], ...entries];
        },
        hover(line, start, end) {
            const hover = hovers.get(line);
            return hover && start >= hover.start && end <= hover.start + hover.length ? hover.content : null;
        },
    };
}

function expressionText(expression: ExpressionSyntax): string {
    switch (expression.kind) {
        case 'LiteralExpressionSyntax': return typeof expression.value === 'object' && expression.value !== null ? expression.value.value : JSON.stringify(expression.value);
        case 'ListExpressionSyntax': return `[${expression.items.map(expressionText).join(', ')}]`;
        case 'ObjectExpressionSyntax': return `{ ${expression.members.map(member => `${JSON.stringify(member.name)}: ${expressionText(member.value)}`).join(', ')} }`;
        case 'RawExpressionSyntax': return expression.text;
        case 'CaseValueExpressionSyntax': return `case.${expression.parameter}`;
        case 'PathExpressionSyntax': return expression.path;
        case 'ContextExpressionSyntax': return `$context.${expression.path}`;
        case 'IdentityExpressionSyntax': return `$identity.${expression.path}`;
        case 'EventContextExpressionSyntax': return `$eventContext.${expression.path}`;
        case 'RefusalExpressionSyntax': return `$refusal.${expression.member}`;
        case 'EventSourceIdExpressionSyntax': return '$eventSourceId';
        case 'CausedByExpressionSyntax': return `$causedBy${expression.property ? `.${expression.property}` : ''}`;
        case 'EnvironmentExpressionSyntax': return `$env.${expression.name}`;
        case 'StringsExpressionSyntax': return `$strings.${expression.key}`;
        case 'SourceItemExpressionSyntax': return `$.${expression.path}`;
        case 'TemplateExpressionSyntax': return '`' + expression.parts.map(part => part.kind === 'TemplateTextSyntax' ? part.text : '${' + expressionText(part.expression) + '}').join('') + '`';
    }
}
