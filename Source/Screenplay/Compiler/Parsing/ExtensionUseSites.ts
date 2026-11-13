// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExtensionComponent, ExtensionContract } from '../Syntax/ExtensionContract';
import { ScreenComponentSyntax } from '../Syntax/Screens';
import { ApplicationSyntax } from '../Syntax/Structure';
import { componentSignalUses } from './ComponentSignalUses';

function* placedComponents(node: unknown): Generator<ScreenComponentSyntax> {
    if (Array.isArray(node)) {
        for (const item of node) yield* placedComponents(item);
        return;
    }
    if (node === null || typeof node !== 'object') return;
    if ((node as { kind?: unknown }).kind === 'ScreenComponentSyntax') yield node as ScreenComponentSyntax;
    for (const value of Object.values(node)) yield* placedComponents(value);
}

function checkPlacement(application: ApplicationSyntax, placed: ScreenComponentSyntax, component: ExtensionComponent, diagnostics: Diagnostic[]): void {
    for (const property of placed.properties) {
        const name = property.property.split('.')[0];
        if (!component.properties.some(declared => declared.name === name)) {
            diagnostics.push({ severity: 'error', code: DiagnosticCodes.UnknownExtensionComponentProperty, message: `Component '${placed.component}' has no property '${name}'.`, location: property.location });
        }
    }

    for (const use of componentSignalUses.get(placed) ?? []) {
        const signal = component.signals.find(candidate => candidate.name === use.trigger);
        const declared = (application.declaredTriggers ?? []).some(trigger => trigger.name === use.trigger);
        if (signal === undefined && !use.trigger.includes('.') && /^[a-z]/.test(use.trigger) && !declared) {
            diagnostics.push({ severity: 'error', code: DiagnosticCodes.UnknownExtensionSignal, message: `Component '${placed.component}' raises no signal '${use.trigger}'.`, location: use.location });
        }

        for (const read of use.reads) {
            if (signal?.values.some(carried => carried.name === read.value) !== true) {
                const detail = signal === undefined ? 'the interaction handles no signal' : `signal '${signal.name}' carries no value '${read.value}'`;
                diagnostics.push({ severity: 'error', code: DiagnosticCodes.UnknownExtensionSignalValue, message: `'signal.${read.value}' reads a signal value, but ${detail}.`, location: read.location });
            }
        }
    }

    if (component.platforms.length === 0) return;
    const targets = new Set((application.designProfiles ?? []).flatMap(profile => profile.platforms ?? []));
    for (const platform of targets) {
        if (!component.platforms.includes(platform)) {
            diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ExtensionComponentPlatformUnsupported, message: `Component '${placed.component}' does not support platform '${platform}', which a ui profile targets.`, location: placed.location });
        }
    }
}

/**
 * Checks each component a screen places from a system that names an extension against that extension's contract: the
 * component, its properties, the signals it handles and the platforms a ui profile targets.
 */
export function checkExtensionUseSites(application: ApplicationSyntax, contracts: ReadonlyMap<string, ExtensionContract>): readonly Diagnostic[] {
    const diagnostics: Diagnostic[] = [];
    const systems = new Map((application.systems ?? []).filter(system => system.extension !== undefined).map(system => [system.name, system]));
    if (systems.size === 0 || contracts.size === 0) return diagnostics;
    for (const placed of placedComponents(application.modules)) {
        const parts = placed.component.split('.');
        if (parts.length !== 2) continue;
        const system = systems.get(parts[0]);
        const contract = system?.extension === undefined ? undefined : contracts.get(system.extension.reference);
        if (system === undefined || contract === undefined) continue;
        const component = contract.components.find(candidate => candidate.name === parts[1]);
        if (component === undefined) {
            diagnostics.push({ severity: 'error', code: DiagnosticCodes.UnknownExtensionComponent, message: `Extension '${system.extension?.reference}' declares no component '${parts[1]}', placed as '${placed.component}'.`, location: placed.location });
        } else {
            checkPlacement(application, placed, component, diagnostics);
        }
    }

    return diagnostics;
}
