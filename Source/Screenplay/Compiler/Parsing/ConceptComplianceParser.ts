// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConceptAttributeSyntax } from '../Syntax/Declarations';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export const specialCategories = ['racialOrEthnicOrigin', 'politicalOpinions', 'religiousOrPhilosophicalBeliefs', 'tradeUnionMembership', 'genetic', 'biometric', 'health', 'sexLifeOrSexualOrientation'];
const directivePattern = pattern('^(@?[a-z_]\\w*)\\s+(reason|scope|special|criminal)(?:\\s+(.*))?$');
const reasonPattern = pattern(`^"(${stringBodyPattern})"$`);

export function complianceWireName(marker: string): string {
    const name = marker.replace(/^@/, '');
    return name === 'personal' ? 'pii' : name === 'secret' ? 'sensitive' : name;
}

export function canonicalComplianceName(marker: string): string {
    const name = complianceWireName(marker);
    return name === 'sensitive' ? 'secret' : name;
}

export function parseComplianceMarkers(context: ParserContext, line: SourceLine, text: string): ConceptAttributeSyntax[] {
    const markers = text.trim().split(/\s+/).filter(marker => marker.length > 0);
    reportLegacy(context, line, markers);
    return markers.map(marker => {
        const name = complianceWireName(marker);
        if (!['pii', 'sensitive'].includes(name) || marker === '@personal' || marker === '@secret') {
            context.error(DiagnosticCodes.UnknownComplianceMarker, `Unknown concept marker '${marker}' - expected pii, personal or secret`, locationOf(line));
        }
        return { kind: 'ConceptAttributeSyntax', name, reason: null, scope: null, specialCategory: null, criminal: false, location: locationOf(line) };
    });
}

export function parseComplianceDirective(context: ParserContext, line: SourceLine, concept: string, attributes: ConceptAttributeSyntax[]): boolean {
    const match = directivePattern.exec(line.content);
    if (match === null) return false;
    const [, marker, setting] = match;
    const value = match[3] ?? '';
    const name = complianceWireName(marker);
    const canonical = canonicalComplianceName(marker);
    reportLegacy(context, line, [marker]);
    if (!['pii', 'sensitive'].includes(name) || marker === '@personal' || marker === '@secret') {
        context.error(DiagnosticCodes.UnknownComplianceMarker, `Unknown concept marker '${marker}' - expected pii, personal or secret`, locationOf(line));
        return true;
    }
    const index = attributes.findIndex(attribute => attribute.name === name);
    if (index < 0) {
        context.error(DiagnosticCodes.AttributeReasonWithoutAttribute, `Concept '${concept}' declares '${canonical} ${setting}' without the marker - write 'concept ${concept} : <Type> ${canonical}'`, locationOf(line));
        return true;
    }
    const attribute = attributes[index];
    switch (setting) {
        case 'reason': {
            const reason = reasonPattern.exec(value);
            if (reason === null) {
                context.error(DiagnosticCodes.UnknownConceptDirective, `Invalid '${canonical} reason' - expected '${canonical} reason "<text>"'`, locationOf(line));
                return true;
            }
            if (attribute.reason !== null) {
                context.error(DiagnosticCodes.DuplicateAttributeReason, `Concept '${concept}' already declares a reason for '${canonical}' - at most one is allowed`, locationOf(line));
                return true;
            }
            attributes[index] = { ...attribute, reason: unescapeString(reason[1]) };
            return true;
        }
        case 'scope':
            if (name !== 'sensitive' || !['subject', 'namespace', 'global'].includes(value)) {
                context.error(DiagnosticCodes.InvalidSecretScope, `Invalid '${canonical} scope ${value}' - scope belongs to secret and must be subject, namespace or global`, locationOf(line));
                return true;
            }
            if (attribute.scope != null) {
                context.error(DiagnosticCodes.DuplicateSecretScope, `Concept '${concept}' already declares secret scope - at most one is allowed`, locationOf(line));
                return true;
            }
            attributes[index] = { ...attribute, scope: value };
            if (attributes.some(attribute => attribute.name === 'pii')) {
                context.warning(DiagnosticCodes.SecretScopeIgnoredForPii, `Concept '${concept}' is pii secret - secret scope is ignored because only Chronicle [PII] renders`, locationOf(line));
            }
            return true;
        case 'special':
        case 'criminal':
            if (name !== 'pii' || (setting === 'special' ? !specialCategories.includes(value) : value.length > 0)) {
                context.error(DiagnosticCodes.InvalidPersonalDataQualifier, `Invalid '${canonical} ${setting}${value.length > 0 ? ` ${value}` : ''}' - expected 'pii criminal' or 'pii special <category>' with category ${specialCategories.join(', ')}`, locationOf(line));
                return true;
            }
            if (setting === 'special' && attribute.specialCategory != null) {
                context.error(DiagnosticCodes.DuplicateSpecialCategory, `Concept '${concept}' already declares pii special - at most one is allowed`, locationOf(line));
                return true;
            }
            attributes[index] = setting === 'special' ? { ...attribute, specialCategory: value } : { ...attribute, criminal: true };
            return true;
        default:
            return false;
    }
}

function reportLegacy(context: ParserContext, line: SourceLine, markers: readonly string[]): void {
    if (markers.some(marker => ['@pii', 'sensitive', '@sensitive'].includes(marker))) {
        context.information(DiagnosticCodes.LegacyComplianceMarker, 'Legacy compliance spelling - use bare pii for personal data and secret for operational secrets; reason text is preserved', locationOf(line));
    }
}
