// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { namedRuleContext } from './named-rule-context';
import { eventSourceHover } from './event-source-authoring';
import { exampleHover } from './example-authoring';
import { operationHover } from './operation-authoring';
import { DocumentSymbols } from './symbols';
import { responseAnalysis, responseAvailability } from './response-analysis';
import { enclosingChain, fenceMap, indentOf, withoutComment } from './document-context';
import { directBody, propertyTypeReference, scanDocument } from './symbols';
import { typeReferenceText } from './TypeReferenceSymbol';
import { eventAnalysisSource } from './event-analysis-source';
import { getSubLanguage } from './sub-language-registry';
import { attributeDocs, contextVariableDocs, handlerIntentDocs, keywordDocs, specificationKeywordDocs } from './keyword-docs';

// Produces the hover markdown for a word at a position, without any editor
// dependency — the Monaco service and the VSCode extension share this content.
// Columns are 1-based, matching both editors' word-at-position APIs.
export function hoverContent(
    lines: string[],
    lineIndex: number,
    word: string,
    startColumn: number,
    endColumn: number,
    application?: DocumentSymbols,
): string | null {
    const fences = fenceMap(lines);
    if (fences[lineIndex]) return null;

    const line = lines[lineIndex] ?? '';
    if (endColumn - 1 > withoutComment(line).length) return null;
    let inString = false;
    let inTemplate = false;
    for (let index = 0; index < startColumn - 1; index++) {
        if (line[index] === '\\' && inString) index++;
        else if (line[index] === '"' && !inTemplate) inString = !inString;
        else if (line[index] === '`' && !inString) inTemplate = !inTemplate;
    }
    if (inString || inTemplate) return null;
    const fixtureHover = exampleHover(lines, lineIndex, startColumn, endColumn, application);
    if (fixtureHover) return fixtureHover;
    const sourceHover = eventSourceHover(lines, lineIndex, startColumn, endColumn, application);
    if (sourceHover) return sourceHover;
    const operation = operationHover(lines, lineIndex, startColumn, endColumn, application);
    if (operation) return operation;
    const tokenAt = (column: number, name: string): boolean => {
        const start = column + (line[column - 1] === '@' ? 1 : 0);
        return word === name && startColumn === start && endColumn === start + name.length && line.slice(start - 1, endColumn - 1) === name;
    };
    const before = line.charAt(startColumn - 2);

    if (before === '@') {
        const doc = attributeDocs[word];
        if (doc) return doc;
    }
    if (before === '$' || before === '.') {
        const variable = line.substring(0, endColumn - 1).match(/\$[\w.]*$/)?.[0];
        if (variable) {
            const doc =
                contextVariableDocs[variable] ??
                (variable.startsWith('$env') ? contextVariableDocs['$env'] : undefined);
            if (doc) return doc;
        }
    }

    // Do not mistake a property/type named optional for the contextual modifier.
    const prefix = line.slice(0, startColumn - 1);
    // Commit to query-parameter context before matching the name and type.
    const followsPropertyType = /^\s*(?:by|filter)\s+/.test(prefix)
        ? /^\s*(?:by|filter)\s+[a-z_]\w*\s+[\w.]+(?:\[\])?\s+$/.test(prefix)
        : /^\s*@?[a-z_]\w*\s+[\w.]+(?:\[\])?\s+$/.test(prefix);
    const followsQueryType = /^\s*query\s+\w+\s*=>\s*(?:observable\s+)?[\w.]+(?:\[\])?\s+$/.test(prefix) &&
        !/^\s*query\s+\w+\s*=>\s*observable\s+$/.test(prefix);
    if (word === 'optional' && (followsPropertyType || followsQueryType)) {
        return `**optional** — ${keywordDocs.optional}`;
    }

    if (word === 'implementation' || word === 'hint') {
        const chain = enclosingChain(lines, fences, lineIndex, indentOf(line));
        const ruleContext = namedRuleContext(lines, lineIndex, indentOf(line));
        if ((word === 'implementation' && ruleContext === 'rule' && /^\s*implementation\s*$/.test(withoutComment(line))) ||
            (word === 'hint' && ruleContext === 'implementation' && /^\s*hint\s+"/.test(line))) {
            return `**${word}** — Command named-rule guidance with at most one file or tagged fence. No payload means pending and fails binding (PLAY0268). Attached predicates keep their existing pure RuleContext contract; reference execution is unsupported, never confirmation.`;
        }
        if (word === 'implementation' && chain[0] === 'handler' && /^\s*implementation\s*$/.test(withoutComment(line))) {
            return `**implementation** — ${handlerIntentDocs.implementation}`;
        }
        if (word === 'hint' && chain[0] === 'implementation' && chain[1] === 'handler' && /^\s*hint\s+"/.test(line)) {
            return `**hint** — ${handlerIntentDocs.hint}`;
        }
    }

    const symbols = scanDocument(lines);
    const owner = symbols.commands.filter(command => command.line < lineIndex).at(-1);
    const property = owner?.properties.find(property => property.name === word && property.line === lineIndex);
    if (property?.isGenerated || (word === 'generated' && owner?.properties.some(property => property.isGenerated && property.line === lineIndex) && followsPropertyType)) {
        return `**${property?.name ?? 'generated'}** — ${property ? typeReferenceText(propertyTypeReference(property)) + '. ' : ''}Generated value, not a request or form input. ${responseAvailability}`;
    }
    const response = owner?.response;
    if (response?.location.line === lineIndex + 1 && (tokenAt(response.location.column, 'returns') || (response.kind === 'ScalarCommandResponseSyntax' && tokenAt(response.source.location.column, response.source.property)))) {
        const source = response.kind === 'ScalarCommandResponseSyntax' ? owner?.properties.find(property => property.name === response.source.property) : undefined;
        return `**returns**${source ? ` — ${typeReferenceText(propertyTypeReference(source))}` : ' — unnamed record response'}. ${responseAvailability}`;
    }
    if (response?.kind === 'RecordCommandResponseSyntax') {
        const field = response.fields.find(field => field.location.line === lineIndex + 1 && (tokenAt(field.location.column, field.name) || tokenAt(field.source.location.column, field.source.property)));
        if (field) {
            const source = owner?.properties.find(property => property.name === field.source.property);
            const type = field.type ? typeReferenceText(field.type) : source ? typeReferenceText(propertyTypeReference(source)) : 'Unresolved type';
            return `**${field.name}** — ${type}${field.type ? ' (explicit)' : ' (inferred)'} = ${field.source.property}. ${source?.isGenerated ? 'Generated source, not request input. ' : ''}${responseAvailability}`;
        }
    }
    for (const specification of responseAnalysis(lines).specifications.values()) {
        if ((word === 'returns' && specification.thenReturns?.location.line === lineIndex + 1) || (word === 'generated' && specification.when?.generatedValues?.some(value => value.location.line === lineIndex + 1))) return responseAvailability;
    }

    const concept = symbols.concepts.find((candidate) => candidate.name === word);
    if (concept) {
        const attributes = concept.attributes.length ? ` ${concept.attributes.join(' ')}` : '';
        const values = concept.enumValues.length
            ? `\n\nValues: ${concept.enumValues.join(', ')}`
            : '';
        const reasons = Object.entries(concept.attributeReasons)
            .map(([attribute, reason]) => `\n\n**@${attribute}** — ${reason}`)
            .join('');
        return `\`\`\`screenplay\nconcept ${concept.name} : ${concept.primitive}${attributes}\n\`\`\`${reasons}${values}`;
    }

    const type = symbols.types.find((candidate) => candidate.name === word);
    if (type) {
        const properties = type.properties
            .map((property) => `${property.name} ${typeReferenceText(propertyTypeReference(property))}`)
            .join('\n');
        return `\`\`\`screenplay\ntype ${type.name}\n${properties}\n\`\`\``;
    }

    const policy = symbols.policies.find((candidate) => candidate.name === word);
    if (policy) {
        const body = policy.requires.length ? policy.requires.join('\n') : '(custom C#)';
        return `\`\`\`screenplay\npolicy ${policy.name}\n${body}\n\`\`\``;
    }

    const event = symbols.events.find((candidate) => candidate.name === word && candidate.line === lineIndex) ??
        symbols.events.filter((candidate) => candidate.name === word)
            .sort((left, right) => (right.generation ?? 1) - (left.generation ?? 1))[0];
    if (event) {
        const properties = event.properties
            .map((property) => `${property.name} ${typeReferenceText(propertyTypeReference(property))}`)
            .join('\n');
        return `\`\`\`screenplay\nevent ${event.name}${event.generation !== undefined ? ` generation ${event.generation}` : ''}\n${properties}\n\`\`\``;
    }

    const command = symbols.commands.find((candidate) => candidate.name === word);
    if (command && command.properties.length > 0) {
        const properties = command.properties
            .map(
                (property) =>
                    `${property.name} ${typeReferenceText(propertyTypeReference(property))}${property.isGenerated ? ' generated' : ''}${property.isIdentifier ? ' identifier' : ''}`,
            )
            .join('\n');
        return `\`\`\`screenplay\ncommand ${command.name}\n${properties}\n\`\`\`${command.response || command.properties.some(property => property.isGenerated) ? `\n\n${responseAvailability}` : ''}`;
    }

    // Sub-language keywords take precedence inside their construct.
    const chain = enclosingChain(lines, fences, lineIndex, indentOf(line));
    for (const construct of [line.trim().split(/\s+/)[0], ...chain]) {
        const subLanguage = getSubLanguage(construct ?? '');
        const doc = subLanguage?.hovers?.[word];
        if (doc) return doc;
    }

    if (word === 'reads' && (chain[0] === 'every' || chain[0] === 'at') && chain.includes('reaction')) {
        return '**reads** — Declares a whole view this clock trigger consults: `reads <View>`. Clock triggers take no values, so `by` is unavailable.';
    }

    const step = line.trim().match(/^(given|when|then)\s+(?:no\s+)?(\w+)/);
    if (step && step[2] === word && chain[0] === 'specification' && specificationKeywordDocs[word]) {
        return `**${word}** — ${specificationKeywordDocs[word]}`;
    }

    if (word === 'id' || word === 'documentation') {
        const source = eventAnalysisSource(lines);
        const inEvent = symbols.events.some(event => directBody(source, fences, event.line, indentOf(source[event.line])).includes(lineIndex));
        const directive = word === 'id' ? /^\s*id\s+"/.test(source[lineIndex]) : /^\s*documentation\s*$/.test(source[lineIndex]);
        if (!inEvent || !directive || startColumn !== indentOf(line) + 1) return null;
    }

    if (word === 'optional' || word === 'generated' || word === 'returns') return null;
    const keywordDoc = keywordDocs[word];
    if (keywordDoc) return `**${word}** — ${keywordDoc}`;

    return null;
}
