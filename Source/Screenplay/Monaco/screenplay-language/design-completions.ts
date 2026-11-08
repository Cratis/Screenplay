// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { indentOf, withoutComment } from './document-context';
import { CompletionEntry } from './completion-items';
import { themeItems } from './scope-items';

// The closed vocabularies of a design system, mirroring Cratis.Screenplay.Design.DesignVocabulary.
export const designCategories = ['color', 'space', 'size', 'radius', 'border', 'shadow', 'font', 'type', 'motion', 'opacity', 'z'];
export const designParts = ['text', 'background', 'border', 'radius', 'height', 'padding', 'shadow', 'font'];
export const designStates = ['default', 'hover', 'focus', 'active', 'disabled', 'selected'];

const partCategories: Record<string, string[]> = {
    text: ['color'],
    background: ['color'],
    border: ['color', 'border'],
    radius: ['radius'],
    height: ['size', 'space'],
    padding: ['space', 'size'],
    shadow: ['shadow'],
    font: ['font', 'type'],
};

const defaultModes = ['light', 'dark'];

interface DesignSystemFacts {
    name: string;
    modes: string[];
    tokens: { category: string; token: string; description?: string }[];
}

interface ThemeFacts {
    name: string;
    implementsName?: string;
    palette: string[];
}

export interface DesignFacts {
    designSystem?: DesignSystemFacts;
    themes: ThemeFacts[];
}

// The lines nested under the block that opens at the given index, as [index, indent] pairs.
function bodyOf(lines: readonly string[], start: number): { index: number; indent: number; text: string }[] {
    const opener = indentOf(lines[start]);
    const body: { index: number; indent: number; text: string }[] = [];
    for (let index = start + 1; index < lines.length; index++) {
        const text = withoutComment(lines[index]);
        if (text.trim().length === 0) continue;
        const indent = indentOf(text);
        if (indent <= opener) break;
        body.push({ index, indent, text: text.trim() });
    }
    return body;
}

// Reads the design system and the themes out of the document's lines - enough for completion, tolerant of
// half-typed text. The compiler is the authority; this only offers names the author already declared.
export function scanDesign(lines: readonly string[]): DesignFacts {
    const facts: DesignFacts = { themes: [] };
    lines.forEach((line, start) => {
        if (/^design\s+system\s+\S/.test(line)) {
            const name = line.match(/^design\s+system\s+(\S+)/)![1];
            const system: DesignSystemFacts = { name, modes: [], tokens: [] };
            const body = bodyOf(lines, start);
            const childIndent = body.length > 0 ? Math.min(...body.map(entry => entry.indent)) : 0;
            body.forEach((entry, position) => {
                if (entry.indent !== childIndent) return;
                const modes = entry.text.match(/^modes\s+(.+)$/);
                if (modes) system.modes = modes[1].split(',').map(mode => mode.trim()).filter(mode => mode.length > 0);
                if (entry.text !== 'tokens') return;
                const end = body.findIndex((candidate, offset) => offset > position && candidate.indent <= childIndent);
                const section = body.slice(position + 1, end < 0 ? undefined : end);
                const categoryIndent = section.length > 0 ? Math.min(...section.map(candidate => candidate.indent)) : 0;
                let category = '';
                for (const candidate of section) {
                    if (candidate.indent === categoryIndent) {
                        category = candidate.text.split(/\s+/)[0];
                        continue;
                    }
                    const token = candidate.text.match(/^([A-Za-z_][\w-]*)(?:\s+"((?:[^"\\]|\\.)*)")?\s*$/);
                    if (token && category.length > 0) system.tokens.push({ category, token: token[1], description: token[2] });
                }
            });
            facts.designSystem = system;
        } else if (/^theme\s+\S/.test(line)) {
            const theme: ThemeFacts = { name: line.match(/^theme\s+(\S+)/)![1], palette: [] };
            const body = bodyOf(lines, start);
            const childIndent = body.length > 0 ? Math.min(...body.map(entry => entry.indent)) : 0;
            let inPalette = false;
            for (const entry of body) {
                if (entry.indent === childIndent) {
                    inPalette = entry.text === 'palette';
                    const implemented = entry.text.match(/^implements\s+(\S+)/);
                    if (implemented) theme.implementsName = implemented[1];
                } else if (inPalette) {
                    const name = entry.text.match(/^([A-Za-z_][\w-]*)\s*=/);
                    if (name) theme.palette.push(name[1]);
                }
            }
            facts.themes.push(theme);
        }
    });
    return facts;
}

const entry = (label: string, documentation: string, insertText: string = label): CompletionEntry => ({ label, insertText, documentation });

const tokenAddresses = (facts: DesignFacts, categories?: string[]): CompletionEntry[] =>
    (facts.designSystem?.tokens ?? [])
        .filter(token => categories === undefined || categories.includes(token.category))
        .map(token => entry(`${token.category}.${token.token}`, token.description ?? `The ${token.category} token ${token.token}.`));

const partEntries = (documentation: string): CompletionEntry[] => designParts.map(part => entry(part, documentation, `${part} = \${1:category.token}`));

const designSystemSections: CompletionEntry[] = [
    entry('modes', 'The modes a theme supplies color values for.', 'modes ${1:light}, ${2:dark}'),
    entry('default theme', 'The theme the application uses unless its ui profile picks another.', 'default theme ${1:Theme}'),
    entry('tokens', 'The design tokens, by category.', 'tokens\n    ${1|color,space,size,radius,border,shadow,font,type,motion,opacity,z|}\n        ${2:token} "${3:Description}"'),
    entry('components', 'The components with their parts and variants.', 'components\n    ${1:Button}\n        parts ${2:text, background}\n        variant ${3:primary}\n            ${4:text} = ${5:color.token}'),
    entry('words', 'The voice and naming of the product.', 'words\n    voice\n        ${1:tone} = "${2:Plain and warm}"'),
    entry('rules', 'Rules a theme must satisfy.', 'rules\n    contrast ${1:text} on ${2:background} at least ${3:4.5}'),
];

/**
 * Completion for the lines of a `design system` and of the `palette`, `values` and mode blocks of a `theme`.
 * Returns null when the position is in neither, so the generic scopes answer.
 */
export function designCompletions(lines: readonly string[], textBefore: string, chain: readonly string[]): CompletionEntry[] | null {
    const root = chain[chain.length - 1];
    if (root !== 'design' && root !== 'theme') return null;
    const facts = scanDesign(lines);
    const text = textBefore.trimStart();
    const afterEquals = /=\s*[\w.]*$/.test(text);

    if (root === 'design') {
        if (/^default\s+theme\s+\w*$/.test(text)) return facts.themes.map(theme => entry(theme.name, 'A declared theme.'));
        if (/^modes\s/.test(text)) return defaultModes.map(mode => entry(mode, 'A mode.'));
        const [innermost, parent] = chain;
        if (chain.length === 1) return designSystemSections.filter(section => !lines.some(line => new RegExp(`^\\s+${section.label.replace(' ', '\\s+')}\\b`).test(line)));
        if (innermost === 'tokens') return designCategories.map(category => entry(category, `A ${category} token category.`, `${category}\n    \${1:token} "\${2:Description}"`));
        if (parent === 'tokens') return [];
        if (innermost === 'components') return [entry('component', 'A component with its parts and variants.', '${1:Button}\n    parts ${2:text, background}\n    variant ${3:primary}\n        ${4:text} = ${5:color.token}')];
        if (innermost === 'words') return [entry('voice', 'How the product speaks.', 'voice\n    ${1:key} = "${2:text}"'), entry('naming', 'How things are named.', 'naming\n    ${1:key} = "${2:text}"')];
        if (innermost === 'voice' || innermost === 'naming') return [];
        if (/^contrast\s+(?:\w+\s+on\s+)?\w*$/.test(text)) return designParts.map(part => entry(part, 'A component part.'));
        if (innermost === 'rules') return [entry('contrast', 'A minimum contrast ratio between two parts.', 'contrast ${1:text} on ${2:background} at least ${3:4.5}')];
        if (chain.includes('components')) {
            if (/^parts\s/.test(text)) return designParts.map(part => entry(part, 'A component part.'));
            const equals = text.match(/^(\w+)\s*=\s*[\w.]*$/);
            if (equals) return tokenAddresses(facts, partCategories[equals[1]]);
            if (innermost === 'variant') return [...partEntries('Sets the part to a token.'), entry('state', 'Overrides parts for a state.', `state \${1|${designStates.join(',')}|}\n    \${2:text} = \${3:color.token}`)];
            if (innermost === 'state') return partEntries('Sets the part to a token for this state.');
            return [entry('parts', 'The parts a variant can style.', 'parts ${1:text, background}'), entry('variant', 'A named look of the component.', 'variant ${1:primary}\n    ${2:text} = ${3:color.token}')];
        }
        return null;
    }

    const themeName = lines.map(line => line.match(/^theme\s+(\S+)/)?.[1]).filter((name): name is string => name !== undefined).pop();
    const theme = facts.themes.find(candidate => candidate.name === themeName) ?? facts.themes[facts.themes.length - 1];
    if (/^implements\s+\w*$/.test(text)) return facts.designSystem === undefined ? [] : [entry(facts.designSystem.name, 'The declared design system.')];
    const modes = facts.designSystem?.modes ?? [];
    if (chain.length === 1) {
        if (!/^\w*$/.test(text)) return null;
        return [...themeItems, ...modes.map(mode => entry(mode, `The ${mode} mode: token values that differ in it.`, `${mode}\n    \${1:color.token} = "\${2:value}"`))];
    }
    if (chain[0] === 'palette') return [];
    if (afterEquals) return [...(theme?.palette ?? []).map(name => entry(`palette.${name}`, 'A palette value.')), ...tokenAddresses(facts)];
    return tokenAddresses(facts).map(address => ({ ...address, insertText: `${address.label} = "\${1:value}"` }));
}
