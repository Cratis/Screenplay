// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, synthesizePersonaCaller } from '@cratis/screenplay-compiler';
import { CompletionEntry } from './completion-items';
import { withoutComment } from './document-context';
import { bmpWordCharacters } from './bmp-word-characters';

const callerPrefix = new RegExp(`^\\s*given caller as\\s+[${bmpWordCharacters}]*$`);
const personaLine = new RegExp(`^\\s*(?:given caller as|persona)\\s+([A-Za-z_][${bmpWordCharacters}]*)\\s*$`);

export function personaAnalysis(syntax: unknown, lines: string[]) {
    const application = syntax as ApplicationSyntax;
    const results = new Map(application.personas.map(persona => [persona.name, synthesizePersonaCaller(persona, application)]));
    return {
        completions(_line: number, before: string): CompletionEntry[] | null {
            if (withoutComment(before).length < before.length || !callerPrefix.test(before)) return null;
            return application.personas.map(persona => ({ label: persona.name, insertText: persona.name, documentation: 'Authenticated deterministic witness of this persona. Negation, needed nonliteral claims, role-URI claims, implementations and policyless personas require an explicit caller.' }));
        },
        hover(line: number, start: number, end: number): string | null {
            const text = withoutComment(lines[line] ?? '');
            const match = personaLine.exec(text);
            if (!match || text.slice(start - 1, end - 1) !== match[1]) return null;
            const result = results.get(match[1]);
            if (!result) return null;
            if (result.refusal) return `**${match[1]} caller** — ${result.refusal.reason}${result.refusal.policy ? ` in policy ${result.refusal.policy}` : ''}. Use an explicit \`given caller\`.`;
            return `**${match[1]} caller** — authenticated.\n\nRoles: ${result.caller!.roles.map(role => JSON.stringify(role)).join(', ') || '(none)'}.\n\nClaims: ${result.caller!.claims.map(claim => `${JSON.stringify(claim.type)} = ${JSON.stringify(claim.value)}`).join(', ') || '(none)'}.\n\nContributions: ${result.contributions.map(atom => `${atom.policy}: ${atom.kind}${atom.type ? ` ${JSON.stringify(atom.type)}` : ''} ${JSON.stringify(atom.value)}`).join('; ')}. This witnesses one deterministic minimal caller, not every caller fitting the persona.`;
        },
    };
}
