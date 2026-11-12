// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { sourceLocation } from '../Diagnostics/SourceLocation';
import {
    ExtensionBackend, ExtensionComponent, ExtensionContract, ExtensionImage, ExtensionRequirement, ExtensionSetting,
    ExtensionSettingExposure, ExtensionSignal, ExtensionTrigger, extensionComponentKinds, extensionPlatforms, extensionRequirementKinds
} from '../Syntax/ExtensionContract';
import { PropertySyntax } from '../Syntax/Declarations';
import { AbilitySyntax, SystemActivationSyntax } from '../Syntax/SystemSyntax';
import { pattern } from '../Text/patterns';
import { unescapeString } from '../Text/StringLiteral';
import { firstWord } from './LineText';
import { parseAbility } from './OperationParser';
import { ParserContext } from './ParserContext';
import { parseProperty, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';
import { parseActivation } from './SystemConfigurationParser';

const text = '"((?:[^"\\\\]|\\\\.)*)"';
const headerPattern = pattern('^extension\\s+([A-Za-z_]\\w*)$');
const displayPattern = pattern(`^display\\s+name\\s+${text}$`);
const descriptionPattern = pattern(`^description\\s+${text}$`);
const iconPattern = pattern(`^icon\\s+${text}$`);
const imagePattern = pattern(`^image\\s+${text}$`);
const captionPattern = pattern(`^caption\\s+${text}$`);
const requiresPattern = pattern('^requires\\s+([a-z]+)\\s+("(?:[^"\\\\]|\\\\.)*"|\\S.*)$');
const settingPattern = pattern('^setting\\s+([A-Za-z_]\\w*)((?:\\s+\\w+)*)$');
const placeholderPattern = pattern(`^placeholder\\s+${text}$`);
const frontendPattern = pattern(`^frontend\\s+${text}$`);
const backendPattern = pattern(`^backend\\s+runtime\\s+([A-Za-z_][\\w.-]*)\\s+(\\S+)\\s+assembly\\s+${text}$`);
const identifierPattern = pattern('^[A-Za-z_]\\w*$');
const declaredOnce = new Set(['display', 'icon', 'backend', 'frontend', 'activation', 'description']);

/** Parses the contract.play of an extension package; the TypeScript counterpart of the C# ExtensionContractParser. */
export function parseExtensionContractDocument(context: ParserContext, path?: string): ExtensionContract | null {
    let contract: ExtensionContract | null = null;
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'extension' && contract === null) contract = parseExtension(context, line);
        else {
            context.error(DiagnosticCodes.InvalidExtensionContract, contract === null ? "A contract file starts with 'extension <Name>'." : "A contract file holds exactly one 'extension' declaration.", locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    if (contract === null && context.diagnostics.length === 0) context.error(DiagnosticCodes.InvalidExtensionContract, "A contract file must declare 'extension <Name>'.", sourceLocation(1, 1, path));
    if (contract !== null) validate(contract, context);
    return contract;
}

function parseExtension(context: ParserContext, header: SourceLine): ExtensionContract {
    const match = headerPattern.exec(header.content);
    if (match === null) context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'extension <Name>'.", locationOf(header));
    let displayName: string | null = null;
    let description: string | null = null;
    let icon: string | null = null;
    let activation: SystemActivationSyntax | null = null;
    let backend: ExtensionBackend | null = null;
    let frontend: string | null = null;
    const images: ExtensionImage[] = [];
    const requirements: ExtensionRequirement[] = [];
    const settings: ExtensionSetting[] = [];
    const abilities: AbilitySyntax[] = [];
    const triggers: ExtensionTrigger[] = [];
    const components: ExtensionComponent[] = [];
    const seen = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (declaredOnce.has(word) && seen.has(word)) {
            context.error(DiagnosticCodes.InvalidExtensionContract, `A contract declares '${word}' once.`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        if (declaredOnce.has(word)) seen.add(word);
        switch (word) {
            case 'display': displayName = oneString(context, line, displayPattern, 'display name "<text>"'); break;
            case 'description': description = oneString(context, line, descriptionPattern, 'description "<text>"'); break;
            case 'icon': icon = oneString(context, line, iconPattern, 'icon "<path>"'); break;
            case 'image': { const image = parseImage(context, line); if (image !== null) images.push(image); break; }
            case 'requires': { const requirement = parseRequirement(context, line); if (requirement !== null) requirements.push(requirement); break; }
            case 'activation': activation = parseActivation(context, line); break;
            case 'setting': { const setting = parseSetting(context, line); if (setting !== null) settings.push(setting); break; }
            case 'ability': abilities.push(parseAbility(context, line)); break;
            case 'trigger': { const trigger = parseTrigger(context, line); if (trigger !== null) triggers.push(trigger); break; }
            case 'component': components.push(parseComponent(context, line)); break;
            case 'backend': backend = parseBackend(context, line); break;
            case 'frontend': frontend = oneString(context, line, frontendPattern, 'frontend "<path>"'); break;
            default:
                context.error(DiagnosticCodes.InvalidExtensionContract, `'${word}' is not a contract line; expected display name, description, icon, image, requires, activation, setting, ability, trigger, component, backend or frontend.`, locationOf(line));
                context.skipBlock(line.indent);
        }
    }
    return { name: match?.[1] ?? '', displayName, description, icon, images, requirements, activation, settings, abilities, triggers, components, backend, frontend, location: locationOf(header) };
}

function rejectChildren(context: ParserContext, line: SourceLine): void {
    const child = context.peekChild(line.indent);
    if (child === undefined) return;
    context.error(DiagnosticCodes.InvalidExtensionContract, 'This contract line is written on one line.', locationOf(child));
    context.skipBlock(line.indent);
}

function oneString(context: ParserContext, line: SourceLine, regex: ReturnType<typeof pattern>, form: string): string | null {
    rejectChildren(context, line);
    const match = regex.exec(line.content);
    if (match !== null) return unescapeString(match[1]);
    context.error(DiagnosticCodes.InvalidExtensionContract, `Expected '${form}'.`, locationOf(line));
    return null;
}

function parseImage(context: ParserContext, line: SourceLine): ExtensionImage | null {
    const match = imagePattern.exec(line.content);
    let caption: string | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const captionMatch = captionPattern.exec(child.content);
        if (captionMatch !== null && caption === null) caption = unescapeString(captionMatch[1]);
        else context.error(DiagnosticCodes.InvalidExtensionContract, "An image takes one optional 'caption \"<text>\"' line.", locationOf(child));
        rejectChildren(context, child);
    }
    if (match !== null) return { path: unescapeString(match[1]), caption, location: locationOf(line) };
    context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'image \"<path>\"'.", locationOf(line));
    return null;
}

function parseRequirement(context: ParserContext, line: SourceLine): ExtensionRequirement | null {
    rejectChildren(context, line);
    const match = requiresPattern.exec(line.content);
    if (match === null || !extensionRequirementKinds.includes(match[1])) {
        context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'requires stage \"<range>\"', 'requires screenplay \"<range>\"', 'requires platform <name>' or 'requires runtime <name> <version>'.", locationOf(line));
        return null;
    }
    const kind = match[1];
    const value = unescapeString(match[2].replace(/^"|"$/g, ''));
    if (kind === 'platform' && !extensionPlatforms.includes(value)) {
        context.error(DiagnosticCodes.UnknownComponentPlatform, `'${value}' is not a platform; expected ${extensionPlatforms.join(', ')}.`, locationOf(line));
    }
    return { kind, value, location: locationOf(line) };
}

function parseSetting(context: ParserContext, line: SourceLine): ExtensionSetting | null {
    rejectChildren(context, line);
    const match = settingPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'setting <name> [public | secret]'.", locationOf(line));
        return null;
    }
    const modifiers = match[2].split(' ').filter(modifier => modifier.length > 0);
    const isPublic = modifiers.includes('public');
    const isSecret = modifiers.includes('secret');
    if (modifiers.some(modifier => modifier !== 'public' && modifier !== 'secret')) context.error(DiagnosticCodes.InvalidExtensionContract, "A setting is 'public', 'secret' or neither.", locationOf(line));
    if (isPublic && isSecret) context.error(DiagnosticCodes.SettingBothPublicAndSecret, `Setting '${match[1]}' cannot be both public and secret; a secret must never reach a browser.`, locationOf(line));
    let exposure: ExtensionSettingExposure = 'backend';
    if (isPublic) exposure = 'public';
    if (isSecret) exposure = 'secret';
    return { name: match[1], exposure, location: locationOf(line) };
}

function named(keyword: string, line: SourceLine): RegExpExecArray | null {
    return pattern(`^${keyword}\\s+([A-Za-z_]\\w*)$`).exec(line.content);
}

function parseFields(context: ParserContext, header: SourceLine, owner: string): PropertySyntax[] {
    const fields: PropertySyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (tryParseProperty(line) === undefined) {
            context.error(DiagnosticCodes.InvalidExtensionContract, `Expected '<field> <Type>' in a ${owner}.`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        const property = parseProperty(context, line);
        if (property !== undefined) fields.push(property);
        rejectChildren(context, line);
    }
    return fields;
}

function parseTrigger(context: ParserContext, header: SourceLine): ExtensionTrigger | null {
    const match = named('trigger', header);
    const payload = parseFields(context, header, 'trigger');
    if (match !== null) return { name: match[1], payload, location: locationOf(header) };
    context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'trigger <Name>'.", locationOf(header));
    return null;
}

function parseComponent(context: ParserContext, header: SourceLine): ExtensionComponent {
    const match = named('component', header);
    if (match === null) context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'component <Name>'.", locationOf(header));
    let kind: string | null = null;
    let placeholder: string | null = null;
    const platforms: string[] = [];
    const properties: PropertySyntax[] = [];
    const signals: ExtensionSignal[] = [];
    const invokes: string[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        const rest = line.content.slice(word.length).trim();
        switch (word) {
            case 'kind':
                rejectChildren(context, line);
                kind = rest;
                if (!extensionComponentKinds.includes(rest)) context.error(DiagnosticCodes.UnknownComponentKind, `'${rest}' is not a component kind; expected ${extensionComponentKinds.join(', ')}.`, locationOf(line));
                break;
            case 'platform':
                rejectChildren(context, line);
                for (const platform of rest.split(' ').filter(value => value.length > 0)) {
                    platforms.push(platform);
                    if (!extensionPlatforms.includes(platform)) context.error(DiagnosticCodes.UnknownComponentPlatform, `'${platform}' is not a platform; expected ${extensionPlatforms.join(', ')}.`, locationOf(line));
                }
                break;
            case 'placeholder': placeholder = oneString(context, line, placeholderPattern, 'placeholder "<path>"'); break;
            case 'property': {
                const inner: SourceLine = { ...line, content: rest, indent: line.indent + word.length + 1 };
                if (tryParseProperty(inner) === undefined) {
                    context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'property <name> <Type>'.", locationOf(line));
                    context.skipBlock(line.indent);
                } else {
                    const property = parseProperty(context, inner);
                    if (property !== undefined) properties.push(property);
                    rejectChildren(context, line);
                }
                break;
            }
            case 'raises': {
                const signal = named('raises', line);
                const values = parseFields(context, line, 'signal');
                if (signal !== null) signals.push({ name: signal[1], values, location: locationOf(line) });
                else context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'raises <signal>'.", locationOf(line));
                break;
            }
            case 'invokes':
                rejectChildren(context, line);
                if (identifierPattern.test(rest)) invokes.push(rest);
                else context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'invokes <Ability>'.", locationOf(line));
                break;
            default:
                context.error(DiagnosticCodes.InvalidExtensionContract, `'${word}' is not a component line; expected kind, platform, placeholder, property, raises or invokes.`, locationOf(line));
                context.skipBlock(line.indent);
        }
    }
    return { name: match?.[1] ?? '', kind, platforms, placeholder, properties, signals, invokes, location: locationOf(header) };
}

function parseBackend(context: ParserContext, line: SourceLine): ExtensionBackend | null {
    rejectChildren(context, line);
    const match = backendPattern.exec(line.content);
    if (match !== null) return { runtime: match[1], version: match[2], assembly: unescapeString(match[3]), location: locationOf(line) };
    context.error(DiagnosticCodes.InvalidExtensionContract, "Expected 'backend runtime <name> <version> assembly \"<file>\"'.", locationOf(line));
    return null;
}

function validate(contract: ExtensionContract, context: ParserContext): void {
    for (const [field, present] of [['display name', contract.displayName], ['description', contract.description], ['icon', contract.icon]] as const) {
        if (present === null || present.trim().length === 0) context.error(DiagnosticCodes.MissingExtensionManifestField, `Extension '${contract.name}' does not declare its ${field}.`, contract.location);
    }
    const abilities = new Set(contract.abilities.map(ability => ability.name.toLowerCase()));
    for (const component of contract.components) {
        if (component.kind === null) context.error(DiagnosticCodes.InvalidExtensionContract, `Component '${component.name}' declares no kind.`, component.location);
        if (component.kind !== null && component.kind !== 'screenplay' && (component.placeholder === null || component.placeholder.trim().length === 0)) {
            context.error(DiagnosticCodes.ComponentWithoutPlaceholder, `Component '${component.name}' has a frontend but declares no placeholder for design tools to draw.`, component.location);
        }
        for (const invoked of component.invokes.filter(name => !abilities.has(name.toLowerCase()))) {
            context.error(DiagnosticCodes.ComponentInvokesUnknownAbility, `Component '${component.name}' invokes ability '${invoked}', which the contract does not declare.`, component.location);
        }
    }
    duplicates(context, 'ability', contract.abilities);
    duplicates(context, 'trigger', contract.triggers);
    duplicates(context, 'component', contract.components);
    duplicates(context, 'setting', contract.settings);
}

function duplicates(context: ParserContext, kind: string, members: readonly { readonly name: string; readonly location: ExtensionContract['location'] }[]): void {
    const seen = new Set<string>();
    for (const member of members) {
        const key = member.name.toLowerCase();
        if (seen.has(key)) context.error(DiagnosticCodes.DuplicateExtensionMember, `The contract already declares ${kind} '${member.name}'.`, member.location);
        seen.add(key);
    }
}
