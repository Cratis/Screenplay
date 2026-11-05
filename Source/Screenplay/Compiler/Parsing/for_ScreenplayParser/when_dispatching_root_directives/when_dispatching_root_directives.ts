// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';
import { rootDirectiveCases } from './given/cases';

const countKeys = ['policies', 'purposes', 'stickyNotes', 'examples', 'seeds', 'uiProfiles', 'sampleDataPacks', 'themes', 'triggers', 'layouts', 'templates', 'behaviors', 'exposures', 'instanceContributions', 'declaredTriggers', 'systems', 'stateMachines', 'workflows', 'eventSources'];
const presenceKeys = ['authentication', 'onboarding', 'identity', 'context'];

export function snapshot(scope: readonly string[], source: string): string {
    const result = parse(`${source}\nconcept Marker : String\n`, 'fragment.play', scope);
    const application = result.value;
    const bag = application as unknown as Record<string, unknown>;
    const count = (key: string): number => Array.isArray(bag[key]) ? (bag[key] as unknown[]).length : 0;
    const present = (key: string): boolean => bag[key] !== null && bag[key] !== undefined;
    const state = [
        `commandReadMode=${application.commandReadMode ?? 'none'}`,
        `timeZone=${application.timeZone ? `${application.timeZone.id}/${application.timeZone.configurable}` : 'none'}`,
        `reaction=${application.reactionProductionMode ?? 'none'}`,
        `domain=${application.domain?.name ?? 'none'}`,
        `imports=[${application.imports.map(i => `${i.qualifiedName}@${i.origin ?? ''}`).join(',')}]`,
        `fileImports=[${application.fileImports.map(i => i.pattern).join(',')}]`,
        `concepts=[${application.concepts.map(c => c.name).join(',')}]`,
        `types=[${application.types.map(t => t.name).join(',')}]`,
        `personas=[${application.personas.map(p => p.name).join(',')}]`,
        `modules=[${application.modules.map(m => m.name).join(',')}]`,
        `counts={${countKeys.map(key => `${key}:${count(key)}`).join(',')}}`,
        `present={${presenceKeys.map(key => `${key}:${present(key)}`).join(',')}}`
    ].join(' ');
    return [...result.diagnostics.map(d => `${d.severity} ${d.code} ${d.location.line}:${d.location.column} ${d.message}`), state].join('\n');
}

describe('when dispatching root directives', () => {
    it.each(rootDirectiveCases.map(c => [c.name, c] as const))('should keep the diagnostics, recovery and state for %s', (_, c) => {
        snapshot(c.scope, c.source).should.equal(c.expected);
    });
});
