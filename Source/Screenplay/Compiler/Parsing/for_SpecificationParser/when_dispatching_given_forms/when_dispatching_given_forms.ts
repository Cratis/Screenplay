// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../../ScreenplayCompiler';
import type { SpecificationSyntax } from '../../../Syntax/Specifications';
import { givenCases } from './given/cases';

function snapshot(exact: boolean, body: string): string {
    const source = `${exact ? 'numbers exact\n' : ''}specification S\n  ${body.replaceAll('\n', '\n  ')}\n  given Marker\n`;
    const result = parseSpecificationSource(source);
    const specification: SpecificationSyntax = result.value![0];
    const caller = specification.givenCaller ? `${specification.givenCaller.authenticated}/${specification.givenCaller.roles.join('+')}/${specification.givenCaller.claims.length}` : 'none';
    const state = `events=[${specification.given.map(event => event.eventType).join(',')}] readModels=[${specification.givenReadModels.map(readModel => readModel.name).join(',')}] captures=[${specification.givenCaptures.map(capture => capture.capture).join(',')}] generated=[${(specification.givenGenerated ?? []).map(value => `${value.command}.${value.property}`).join(',')}] clock=${specification.givenClock?.instant ?? 'none'} timeZone=${specification.givenTimeZone?.id ?? 'none'} caller=${caller} persona=${specification.givenCallerPersona?.name ?? 'none'} failures=[${(specification.givenOperationFailures ?? []).map(failure => failure.operation).join(',')}]`;
    return [...result.diagnostics.map(diagnostic => `${diagnostic.severity} ${diagnostic.code} ${diagnostic.location.line}:${diagnostic.location.column} ${diagnostic.message}`), state].join('\n');
}

describe('when dispatching given forms', () => {
    it.each(givenCases.map(given => [given.name, given] as const))('should keep the diagnostics, recovery and state for %s', (_, given) => {
        snapshot(given.exact, given.body).should.equal(given.expected);
    });
});
