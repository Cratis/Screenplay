// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConstraintSyntax } from '../Syntax/Constraints';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^constraint\\s+([A-Za-z_]\\w*)$');
const uniqueEventPattern = pattern('^unique\\s+event\\s+([A-Z]\\w*)$');
const uniquePropertyPattern = pattern('^unique\\s+([a-z_][\\w.]*(?:\\s*,\\s*[a-z_][\\w.]*)*)\\s+on\\s+([A-Z]\\w*)$');
const releasePattern = pattern('^released\\s+by\\s+([A-Z]\\w*)$');
const messagePattern = pattern(`^message\\s+"(${stringBodyPattern})"$`);

const targetOf = (rule: ConstraintSyntax): string | undefined => rule.kind === 'FileConstraintSyntax' ? undefined : rule.event;

// The port of the C# ConstraintParser: the first rule is the constraint, any further rules of the same kind
// are its additional rules.
export function parseConstraint(context: ParserContext, line: SourceLine): ConstraintSyntax {
    const headerMatch = header.exec(line.content);
    if (headerMatch === null) {
        context.error(DiagnosticCodes.InvalidConstraintDeclaration, `Invalid constraint declaration '${line.content}' - expected 'constraint <Name>'`, locationOf(line));
    }
    const name = headerMatch?.[1] ?? firstWord(line.content);
    const rules: ConstraintSyntax[] = [];
    const releases: string[] = [];
    const releasedEvents = new Set<string>();
    const targets = new Set<string>();
    let message: string | null = null;
    let ignoreCasing = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const release = releasePattern.exec(child.content);
        const text = messagePattern.exec(child.content);
        if (release !== null) {
            if (releasedEvents.has(release[1])) {
                context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' already releases with event '${release[1]}'`, locationOf(child));
            } else {
                releases.push(release[1]);
                releasedEvents.add(release[1]);
            }
        } else if (text !== null) {
            if (message !== null) {
                context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' already has a message`, locationOf(child));
            } else {
                message = unescapeString(text[1]);
            }
        } else if (child.content === 'ignore casing') {
            if (ignoreCasing) {
                context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' already ignores casing`, locationOf(child));
            }
            ignoreCasing = true;
        } else {
            addRule(context, name, child, rules, targets);
        }
    }
    for (const release of releases.filter(release => targets.has(release))) {
        context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' cannot target and release event '${release}'`, locationOf(line));
    }
    if (rules.length === 0) {
        context.error(DiagnosticCodes.ConstraintWithoutRule, `Constraint '${name}' must declare 'unique ... on ...', 'unique event ...' or 'file ...'`, locationOf(line));
        rules.push(rule(name, locationOf(line), { kind: 'UniqueEventConstraintSyntax', event: '' }));
    }
    if (rules[0].kind === 'FileConstraintSyntax' && (releases.length > 0 || message !== null || ignoreCasing)) {
        context.error(DiagnosticCodes.InvalidConstraintBody, `File constraint '${name}' cannot have declarative options`, locationOf(line));
    }
    return { ...rules[0], additionalRules: rules.slice(1), releasedBy: releases, message, ignoreCasing };
}

function addRule(context: ParserContext, name: string, line: SourceLine, rules: ConstraintSyntax[], targets: Set<string>): void {
    const parsed = parseRule(context, name, line);
    if (parsed === undefined) {
        return;
    }
    if (rules.length > 0 && (parsed.kind === 'FileConstraintSyntax' || rules[0].kind === 'FileConstraintSyntax' ||
        (parsed.kind === 'UniqueEventConstraintSyntax') !== (rules[0].kind === 'UniqueEventConstraintSyntax'))) {
        context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' cannot mix constraint kinds`, locationOf(line));
        return;
    }
    const target = targetOf(parsed);
    if (target !== undefined && targets.has(target)) {
        context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' already targets event '${target}'`, locationOf(line));
        return;
    }
    if (target !== undefined) targets.add(target);
    rules.push(parsed);
}

function parseRule(context: ParserContext, name: string, line: SourceLine): ConstraintSyntax | undefined {
    const location = locationOf(line);
    const uniqueEvent = uniqueEventPattern.exec(line.content);
    if (uniqueEvent !== null) {
        return rule(name, location, { kind: 'UniqueEventConstraintSyntax', event: uniqueEvent[1] });
    }
    const uniqueProperty = uniquePropertyPattern.exec(line.content);
    if (uniqueProperty !== null) {
        const properties = uniqueProperty[1].split(',').map(property => property.trim());
        if (new Set(properties).size !== properties.length) {
            context.error(DiagnosticCodes.DuplicateConstraintBody, `Constraint '${name}' repeats a property in its composite key`, location);
            return undefined;
        }
        return rule(name, location, { kind: 'UniquePropertyConstraintSyntax', property: properties[0], additionalProperties: properties.slice(1), event: uniqueProperty[2] });
    }
    if (isFileDirective(line)) {
        context.warning(DiagnosticCodes.FileConstraintOnlySupportsUniqueness, 'A file constraint can only declare unique constraints in Chronicle - declare them with \'unique ...\' so they are portable; a rule that is not uniqueness belongs in command validation or a \'require\' condition', location);
        const path = line.content.substring('file'.length).trim();
        return rule(name, location, { kind: 'FileConstraintSyntax', file: { kind: 'FileReferenceSyntax', path, location } });
    }
    context.error(DiagnosticCodes.InvalidConstraintBody, `Invalid constraint body '${line.content}'`, location);
    return undefined;
}

type RuleShape =
    | { kind: 'UniqueEventConstraintSyntax'; event: string }
    | { kind: 'UniquePropertyConstraintSyntax'; property: string; additionalProperties: string[]; event: string }
    | { kind: 'FileConstraintSyntax'; file: { kind: 'FileReferenceSyntax'; path: string; location: ReturnType<typeof locationOf> } };

function rule(name: string, location: ReturnType<typeof locationOf>, shape: RuleShape): ConstraintSyntax {
    return { ...shape, name, additionalRules: [], releasedBy: [], ignoreCasing: false, message: null, location } as ConstraintSyntax;
}
