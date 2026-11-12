// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StorylineSyntax } from '../Syntax/Storylines';
import type { AgentsSyntax } from '../Syntax/Agents';
import { parseAgents } from './AgentsParser';
import { parseStoryline } from './StorylineParser';
import { CommandReadMode } from '../Syntax/CommandReadMode';
import { ReactionProductionMode } from '../Syntax/ReactionProductionMode';
import { EnumerationMembers } from './EnumerationMembers';
import { GenerationSyntax } from '../Syntax/GenerationSyntax';
import { misplacedRuleHintForConcept } from './MisplacedRuleHint';
import { parseGeneration } from './GenerationParser';
import { IdentitySyntax } from '../Syntax/IdentitySyntax';
import { parseIdentity } from './IdentityParser';
import { parseApplicationContext } from './ApplicationContextParser';
import { misplacedReactionIdentity } from './ReactionIdentityParser';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { recordAuthoredDocument } from '../Syntax/SourceOptions';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { describePlacement, documentPlacement, isDocumentPlacement, PlayPlacement } from '../Files/PlayPlacement';
import { PersonaSyntax, StickyNoteSyntax, UserSyntax } from '../Syntax/Authorization';
import { SinkSyntax } from '../Syntax/Sinks';
import { parseSinkDeclaration, resolveSinks, sinkKeyword } from './Sinks';
import { ConceptSyntax, DomainSyntax, ImportSyntax, TypeSyntax } from '../Syntax/Declarations';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { TimeZoneSyntax } from '../Syntax/Calendar';
import { ApplicationContextSyntax, ApplicationSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax } from '../Syntax/Structure';
import { parseTriggerDeclaration } from './TriggerDataParser';
import { pattern } from '../Text/patterns';
import { parseComplianceDirective, parseComplianceMarkers } from './ConceptComplianceParser';
import { parseType } from './DeclarationParsers';
import { ValidateSyntax } from '../Syntax/Commands';
import { parseValidate } from './CommandParser';
import { parseDescription } from './DescriptionParser';
import { FeatureBody, featureBodyExpected } from './FeatureBody';
import { isFileImport, parseFileImport } from './FileImportParser';
import { isFileDirective } from './FileReferences';
import { collectInputUses } from './InputUses';
import { firstWord, unescapeIdentifier } from './LineText';
import { ModuleBody, moduleBodyExpected, modulePattern, parseModule } from './ModuleBody';
import { ParserContext } from './ParserContext';
import { PolicySyntax } from '../Syntax/Policies';
import { parsePolicy } from './PolicyParser';
import { PurposeSyntax } from '../Syntax/Purposes';
import { parsePurpose } from './PurposeParser';
import { SeedSyntax } from '../Syntax/Seeds';
import { parseSeed } from './SeedParser';
import { parseSystem } from './OperationParser';
import { parseSecret } from './SystemConfigurationParser';
import { parseStateMachine } from './StateMachineParser';
import { parseWorkflow } from './WorkflowParser';
import { StateMachineSyntax, WorkflowSyntax } from '../Syntax/Lifecycles';
import { EventVisibility } from '../Syntax/EventVisibility';
import { isBlankPublicEventOrigin } from '../Syntax/PublicEventInvariants';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { SecretSyntax, SystemSyntax } from '../Syntax/Operations';
import { EventSourceSyntax } from '../Syntax/EventSources';
import { parseCanvasPair } from './CanvasPlacement';
import { parseEventSource } from './EventSourceParser';
import { parseExposure, parseInstance } from './CompositionParser';
import { ExposureSyntax, InstanceContributionsSyntax } from '../Syntax/CompositionSyntax';
import { parseExample } from './SpecificationExampleParser';
import { parseUiProfile } from './UiProfileLegacyLayout';
import { parseDesignSystem } from './DesignSystemParser';
import { parseTheme } from './ThemeParser';
import type { DesignProfileFacts, DesignSystemSyntax, ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { locationOf, SourceLine, startOf } from './SourceLine';

const domainPattern = pattern('^domain\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)$');
const timeZonePattern = pattern(`^time\\s+zone\\s+(configurable\\s+default\\s+)?"(${stringBodyPattern})"$`);
const importPattern = pattern(`^import\\s+([\\w.]+)(?:\\s+from\\s+"(?<origin>${stringBodyPattern})")?$`);
const conceptPattern = pattern('^concept\\s+(\\w+)\\s*:\\s*(\\w+)((?:\\s+@?\\w+)*)$');
const enumValuePattern = pattern('^@?[a-z_]\\w*$');
const personaPattern = pattern('^persona\\s+([A-Za-z_]\\w*)$');
const userPattern = pattern(`^user\\s+([A-Za-z_]\\w*)(?:\\s+"(${stringBodyPattern})")?(?:\\s+as\\s+([A-Za-z_]\\w*(?:\\s*,\\s*[A-Za-z_]\\w*)*))?$`);
const personaPolicyPattern = pattern('^policy\\s+([A-Za-z_]\\w*)$');
const personaGoalPattern = pattern(`^goal\\s+"(${stringBodyPattern})"$`);
const stickyNotePattern = pattern('^sticky\\s+note\\s+on\\s+(personas|eventsources|givencontexts|seeddata)$');
const stickyNoteColourPattern = pattern('^colour\\s+(yellow|pink|green|blue)$');
const tabIndentPattern = /^[ ]*\t/;
const primitiveTypes = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

// Top-level, module and feature constructs the C# compiler knows that this compiler does not model. They
// are skipped whole, not reported.
const opaqueTopLevel = new Set(['policy', 'authentication', 'onboarding', 'seed', 'ui', 'trigger', 'layout', 'behavior', 'sample']);

// What belongs in a module or feature body; at the top level of a whole document it gets a hint saying so.
const bodyKeywords = new Set(['slice', 'feature', 'description', 'documentation', 'authorize', 'screen', 'dialog', 'form', 'contribute', 'on', 'uses']);

// Parses one document into its application syntax - the port of the C# ScreenplayParser. The placement says
// where the document's top level belongs: the application, unless an import placed it in a module or feature.
export function parseApplication(context: ParserContext, lines: readonly SourceLine[], placement: PlayPlacement = documentPlacement): ApplicationSyntax {
    context.deferIdentityValidation = true;
    warnOnTabIndentation(context, lines);
    const state = createRootState(placement);
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        dispatchRoot(context, line, state);
    }
    return buildApplication(context, state);
}

// What the root loop gathers while it reads the lines of a document.
interface RootState {
    readonly placement: PlayPlacement;
    readonly moduleBody: ModuleBody | undefined;
    readonly featureBody: FeatureBody | undefined;
    readonly placedBody: ModuleBody | FeatureBody | undefined;
    identity: IdentitySyntax | null;
    applicationContext: ApplicationContextSyntax | null;
    reactionProductionMode: ReactionProductionMode | null;
    sawReactionBatches: boolean;
    commandReadMode: CommandReadMode | null;
    timeZone: TimeZoneSyntax | null;
    sawTimeZone: boolean;
    sawCommandReads: boolean;
    domain: DomainSyntax | null;
    sawOtherConstruct: boolean;
    readonly imports: ImportSyntax[];
    readonly fileImports: FileImportSyntax[];
    readonly concepts: ConceptSyntax[];
    readonly types: TypeSyntax[];
    readonly systems: SystemSyntax[];
    readonly secrets: SecretSyntax[];
    readonly stateMachines: StateMachineSyntax[];
    readonly workflows: WorkflowSyntax[];
    readonly eventSources: EventSourceSyntax[];
    readonly modules: ModuleSyntax[];
    readonly personas: PersonaSyntax[];
    readonly users: UserSyntax[];
    readonly storylines: StorylineSyntax[];
    agents: AgentsSyntax | null;
    readonly sinks: SinkSyntax[];
    readonly stickyNotes: StickyNoteSyntax[];
    readonly policies: PolicySyntax[];
    readonly purposes: PurposeSyntax[];
    readonly seeds: SeedSyntax[];
    readonly examples: SpecificationExampleSyntax[];
    readonly declaredTriggers: NonNullable<ApplicationSyntax['declaredTriggers']>[number][];
    readonly exposures: ExposureSyntax[];
    readonly instanceContributions: InstanceContributionsSyntax[];
    readonly themes: ThemeSyntax[];
    readonly designSystems: DesignSystemSyntax[];
    readonly designProfiles: DesignProfileFacts[];
}

interface RootRule {
    readonly matches: (keyword: string, line: SourceLine, state: RootState) => boolean;
    readonly parse: (context: ParserContext, line: SourceLine, state: RootState) => void;
}

function createRootState(placement: PlayPlacement): RootState {
    const moduleBody = placement.length === 1 ? new ModuleBody(placement[0]) : undefined;
    const featureBody = placement.length > 1 ? new FeatureBody(placement[placement.length - 1]) : undefined;
    return {
        placement, moduleBody, featureBody, placedBody: moduleBody ?? featureBody,
        identity: null, applicationContext: null, reactionProductionMode: null, sawReactionBatches: false, commandReadMode: null,
        timeZone: null, sawTimeZone: false, sawCommandReads: false, domain: null, sawOtherConstruct: false,
        imports: [], fileImports: [], concepts: [], types: [], systems: [], secrets: [], stateMachines: [], workflows: [], eventSources: [], modules: [],
        personas: [], users: [], storylines: [], agents: null, sinks: [], stickyNotes: [], policies: [], purposes: [], seeds: [], examples: [], declaredTriggers: [], exposures: [], instanceContributions: [],
        themes: [], designSystems: [], designProfiles: []
    };
}

const isKeyword = (name: string) => (keyword: string): boolean => keyword === name;

// The directives that open a document, tried before the bookkeeping that counts a construct as declared.
const preambleRules: readonly RootRule[] = [
    { matches: (keyword, line) => keyword === 'command' && /^command\s+reads(?:\s|$)/.test(line.content), parse: parseRootCommandReads },
    { matches: (keyword, line) => keyword === 'time' && /^time\s+zone(?:\s|$)/.test(line.content), parse: parseRootTimeZone },
    { matches: isKeyword('reaction'), parse: parseRootReactionBatches },
    { matches: isKeyword('runs'), parse: (context, line) => { misplacedReactionIdentity(context, line); } },
    { matches: isKeyword('domain'), parse: parseRootDomain }
];

// The rest of the root constructs in the order they are tried; a line none of them takes goes to the placed body, then to the unexpected top level report.
const bodyRules: readonly RootRule[] = [
    { matches: (keyword, line) => keyword === 'import' && isFileImport(line.content), parse: parseRootFileImport },
    { matches: isKeyword('import'), parse: parseRootImport },
    { matches: isKeyword('example'), parse: parseRootExample },
    { matches: isKeyword('eventsource'), parse: (context, line, state) => { state.eventSources.push(parseEventSource(context, line)); } },
    { matches: isKeyword('secret'), parse: (context, line, state) => { const secret = parseSecret(context, line); if (secret !== null) state.secrets.push(secret); } },
    { matches: isKeyword('system'), parse: (context, line, state) => { state.systems.push(parseSystem(context, line)); } },
    { matches: isKeyword('statemachine'), parse: (context, line, state) => { state.stateMachines.push(parseStateMachine(context, line)); } },
    { matches: isKeyword('workflow'), parse: (context, line, state) => { state.workflows.push(parseWorkflow(context, line)); } },
    { matches: isKeyword('concept'), parse: (context, line, state) => { state.concepts.push(parseConcept(context, line)); } },
    { matches: isKeyword('type'), parse: (context, line, state) => { state.types.push(parseType(context, line)); } },
    { matches: (keyword, _, state) => keyword === 'module' && !isDocumentPlacement(state.placement), parse: (context, line, state) => parseModuleInPlacedFile(context, line, state.placement, state.moduleBody) },
    { matches: isKeyword('module'), parse: (context, line, state) => { state.modules.push(parseModule(context, line)); } },
    { matches: isKeyword('user'), parse: (context, line, state) => { state.users.push(parseUser(context, line)); } },
    { matches: isKeyword('storyline'), parse: (context, line, state) => { state.storylines.push(parseStoryline(context, line)); } },
    { matches: isKeyword('agents'), parse: (context, line, state) => {
        const agents = parseAgents(context, line);
        if (state.agents !== null) context.error(DiagnosticCodes.DuplicateAgentsDeclaration, 'An application allows exactly one agents block', locationOf(line));
        else state.agents = agents;
    } },
    { matches: isKeyword(sinkKeyword), parse: (context, line, state) => { const sink = parseSinkDeclaration(context, line); if (sink !== undefined) state.sinks.push(sink); } },
    { matches: isKeyword('persona'), parse: (context, line, state) => { state.personas.push(parsePersona(context, line)); } },
    { matches: isKeyword('sticky'), parse: parseRootStickyNote },
    { matches: isKeyword('seed'), parse: (context, line, state) => { state.seeds.push(parseSeed(context.valueContext, line)); } },
    { matches: (keyword, _, state) => keyword === 'purpose' && state.placedBody !== undefined, parse: (context, line, state) => { state.placedBody?.tryParse(context, line); } },
    { matches: isKeyword('purpose'), parse: (context, line, state) => { state.purposes.push(parsePurpose(context, line)); } },
    { matches: isKeyword('context'), parse: (context, line, state) => { state.applicationContext = parseApplicationContext(context, line, state.applicationContext, state.placement); } },
    { matches: isKeyword('identity'), parse: (context, line, state) => { state.identity = parseIdentity(context, line, state.identity); } },
    { matches: isKeyword('policy'), parse: parseRootPolicy },
    { matches: isKeyword('exposure'), parse: (context, line, state) => { state.exposures.push(parseExposure(context, line)); } },
    { matches: isKeyword('instance'), parse: (context, line, state) => { state.instanceContributions.push(parseInstance(context, line)); } },
    { matches: isKeyword('trigger'), parse: parseRootTrigger },
    { matches: isKeyword('design'), parse: (context, line, state) => { addDesignSystem(context, parseDesignSystem(context, line), state.designSystems); } },
    { matches: isKeyword('theme'), parse: (context, line, state) => { addTheme(context, parseTheme(context, line), state.themes); } },
    { matches: keyword => opaqueTopLevel.has(keyword), parse: parseRootOpaque }
];

function dispatchRoot(context: ParserContext, line: SourceLine, state: RootState): void {
    const keyword = firstWord(line.content);
    const preamble = preambleRules.find(rule => rule.matches(keyword, line, state));
    if (preamble !== undefined) {
        preamble.parse(context, line, state);
        return;
    }
    // The C# parser asks whether anything was declared before the domain, so an import it rejected,
    // a file import and an unknown construct do not count.
    state.sawOtherConstruct ||= declaresConstruct(keyword, line, state.placement);
    context.authoredDeclarations ||= keyword !== 'import' && declaresConstruct(keyword, line, state.placement);
    const rule = bodyRules.find(candidate => candidate.matches(keyword, line, state));
    if (rule !== undefined) {
        rule.parse(context, line, state);
    } else if (state.placedBody?.tryParse(context, line) !== true) {
        reportUnexpectedTopLevel(context, line, state.placement);
    }
}

function isRepeatedOrMisplaced(alreadyDeclared: boolean, state: RootState): boolean {
    return alreadyDeclared || !isDocumentPlacement(state.placement);
}

function parseRootCommandReads(context: ParserContext, line: SourceLine, state: RootState): void {
    if (isRepeatedOrMisplaced(state.sawCommandReads, state) || !/^command\s+reads\s+protected$/.test(line.content)) {
        context.error(DiagnosticCodes.InvalidCommandReadsDeclaration, "Declare 'command reads protected' once at the application root.", locationOf(line));
    } else {
        state.commandReadMode = CommandReadMode.Protected;
    }
    state.sawCommandReads = true;
}

function parseRootTimeZone(context: ParserContext, line: SourceLine, state: RootState): void {
    const match = timeZonePattern.exec(line.content);
    context.skipBlock(line.indent);
    if (isRepeatedOrMisplaced(state.sawTimeZone, state) || match === null) {
        context.error(DiagnosticCodes.InvalidTimeZoneDeclaration, "Declare 'time zone \"<id>\"' or 'time zone configurable default \"<id>\"' once at the application root.", locationOf(line));
    } else {
        state.timeZone = { kind: 'TimeZoneSyntax', id: unescapeString(match[2]), configurable: match[1] !== undefined, location: locationOf(line) };
    }
    state.sawTimeZone = true;
}

function parseRootReactionBatches(context: ParserContext, line: SourceLine, state: RootState): void {
    if (isRepeatedOrMisplaced(state.sawReactionBatches, state) || !/^reaction\s+batches\s+atomic$/.test(line.content)) {
        context.error(DiagnosticCodes.InvalidReactionBatchesDeclaration, "Declare 'reaction batches atomic' once at the application root.", locationOf(line));
    } else {
        state.reactionProductionMode = ReactionProductionMode.Atomic;
    }
    state.sawReactionBatches = true;
}

function parseRootDomain(context: ParserContext, line: SourceLine, state: RootState): void {
    context.authoredDeclarations = true;
    state.domain = parseDomain(context, line, state.domain, state.sawOtherConstruct);
}

function parseRootFileImport(context: ParserContext, line: SourceLine, state: RootState): void {
    // A top level import belongs to whatever the document's top level is - the application, or the
    // module or feature the document was itself imported into.
    if (state.placedBody !== undefined) {
        state.placedBody.tryParse(context, line);
    } else {
        parseFileImport(context, line, state.fileImports);
    }
}

function parseRootImport(context: ParserContext, line: SourceLine, state: RootState): void {
    const match = importPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidImportDeclaration, `Invalid import '${line.content}' - expected 'import <Qualified.Name> [from "<origin>"]'`, locationOf(line));
        return;
    }
    const origin = match.groups?.origin === undefined ? null : unescapeString(match.groups.origin);
    if (origin !== null && isBlankPublicEventOrigin(origin)) {
        context.error(DiagnosticCodes.InvalidImportDeclaration, 'An import origin must be a nonblank quoted string', locationOf(line));
    }
    state.imports.push({ kind: 'ImportSyntax', qualifiedName: match[1], origin, visibility: origin === null ? EventVisibility.Private : EventVisibility.Public, location: locationOf(line) });
}

function parseRootExample(context: ParserContext, line: SourceLine, state: RootState): void {
    if (state.placedBody !== undefined) state.placedBody.tryParse(context, line);
    else state.examples.push(parseExample(context, line));
}

function parseRootStickyNote(context: ParserContext, line: SourceLine, state: RootState): void {
    const stickyNote = parseStickyNote(context, line);
    if (stickyNote !== null) state.stickyNotes.push(stickyNote);
}

function parseRootPolicy(context: ParserContext, line: SourceLine, state: RootState): void {
    const policy = parsePolicy(context, line);
    if (state.policies.some(previous => previous.name === policy.name)) {
        context.error(DiagnosticCodes.DuplicateDeclaration, `Duplicate declaration of policy '${policy.name}' - policy names must be unique`, policy.location);
    }
    state.policies.push(policy);
}

function addDesignSystem(context: ParserContext, designSystem: DesignSystemSyntax, designSystems: DesignSystemSyntax[]): void {
    if (designSystems.length > 0) {
        context.error(DiagnosticCodes.DuplicateDesignSystem,
            `The design system '${designSystem.name}' is a second one - an application has exactly one design system, '${designSystems[0].name}'`, designSystem.location);
        return;
    }
    designSystems.push(designSystem);
}

function addTheme(context: ParserContext, theme: ThemeSyntax, themes: ThemeSyntax[]): void {
    if (themes.some(existing => existing.name === theme.name)) {
        context.error(DiagnosticCodes.DuplicateTheme, `A theme named '${theme.name}' is already declared - theme names must be unique`, theme.location);
        return;
    }
    themes.push(theme);
}

function parseRootTrigger(context: ParserContext, line: SourceLine, state: RootState): void {
    const data = parseTriggerDeclaration(context, line);
    const name = /^trigger\s+([A-Za-z_]\w*)$/.exec(line.content)?.[1];
    if (name !== undefined) state.declaredTriggers.push({ name, data });
}

function parseRootOpaque(context: ParserContext, line: SourceLine, state: RootState): void {
    const keyword = firstWord(line.content);
    if (keyword === 'behavior' || keyword === 'layout') collectInputUses(context, line);
    else if (keyword === 'ui') {
        const profile = parseUiProfile(context, line);
        if (profile !== undefined) state.designProfiles.push(profile);
    }
    else context.skipOpaqueBlock(line.indent);
}

// A property the syntax only carries when it was declared.
function presentOnly<K extends string, V>(key: K, value: V | null): { [P in K]?: V } {
    return (value === null ? {} : { [key]: value }) as { [P in K]?: V };
}

// A list the syntax only carries when something was declared in it.
function nonEmptyOnly<K extends string, V>(key: K, values: V[]): { [P in K]?: V[] } {
    return (values.length > 0 ? { [key]: values } : {}) as { [P in K]?: V[] };
}

function buildApplication(context: ParserContext, state: RootState): ApplicationSyntax {
    const { placement, moduleBody, featureBody, modules } = state;
    if (moduleBody !== undefined) {
        modules.unshift(moduleBody.build(context.start, true));
    } else if (featureBody !== undefined) {
        modules.unshift(place(placement, featureBody.build(context.start, true), context.start));
    }
    const { commandReadMode, timeZone, applicationContext, reactionProductionMode, identity, domain, imports, concepts, types, systems, stateMachines, workflows, eventSources, examples, personas, users, storylines, sinks, policies, purposes, seeds, declaredTriggers, fileImports, exposures, stickyNotes, instanceContributions, themes, designSystems, designProfiles } = state;
    const root: ApplicationSyntax = resolveSinks({ kind: 'ApplicationSyntax', ...presentOnly('agents', state.agents), ...presentOnly('commandReadMode', commandReadMode), ...presentOnly('timeZone', timeZone), ...presentOnly('context', applicationContext), ...presentOnly('reactionProductionMode', reactionProductionMode), ...presentOnly('identity', identity), sourceOptions: context.sourceOptions, domain, imports, concepts, types, systems, ...nonEmptyOnly('stateMachines', stateMachines), ...nonEmptyOnly('workflows', workflows), eventSources, examples, modules, personas, policies, purposes, seeds, declaredTriggers, fileImports, ...nonEmptyOnly('exposures', exposures), ...nonEmptyOnly('users', users), ...nonEmptyOnly('storylines', storylines), ...nonEmptyOnly('sinks', sinks), ...nonEmptyOnly('secrets', state.secrets), ...nonEmptyOnly('stickyNotes', stickyNotes), ...nonEmptyOnly('instanceContributions', instanceContributions), ...nonEmptyOnly('themes', themes), ...nonEmptyOnly('designSystems', designSystems), ...nonEmptyOnly('designProfiles', designProfiles), location: context.start }, context);
    if (context.authoredDeclarations) recordAuthoredDocument(root);
    return root;
}

function declaresConstruct(keyword: string, line: SourceLine, placement: PlayPlacement): boolean {
    if (keyword === 'import') {
        return importPattern.test(line.content);
    }
    if (keyword === 'module') {
        return isDocumentPlacement(placement);
    }
    if (keyword === 'identity') return line.content === 'identity';
    if (keyword === 'context') return line.content === 'context';
    return keyword === 'purpose' || keyword === 'example' || keyword === 'exposure' || keyword === 'instance' || keyword === 'eventsource' || keyword === 'system' || keyword === 'statemachine' || keyword === 'workflow' || keyword === 'concept' || keyword === 'type' || keyword === 'persona' || keyword === 'user' || keyword === 'storyline' || keyword === 'sticky' || opaqueTopLevel.has(keyword) || keyword === 'design' || keyword === 'theme';
}

function parseModuleInPlacedFile(context: ParserContext, line: SourceLine, placement: PlayPlacement, moduleBody: ModuleBody | undefined): void {
    const name = modulePattern.exec(line.content)?.[1] ?? '';

    // Restating the module a file is placed in says nothing new, so its body simply joins the placement.
    if (moduleBody !== undefined && name === placement[0]) {
        context.authoredDeclarations = true;
        moduleBody.parseChildren(context, line);
        return;
    }
    context.error(DiagnosticCodes.ModuleInPlacedFile,
        `This file is imported into ${describePlacement(placement)}, so it cannot declare module '${name}' - import it at the top level of a document instead`, locationOf(line));
    context.skipBlock(line.indent);
}

function reportUnexpectedTopLevel(context: ParserContext, line: SourceLine, placement: PlayPlacement): void {
    const word = firstWord(line.content);
    if (!isDocumentPlacement(placement)) {
        const expected = placement.length === 1 ? moduleBodyExpected : featureBodyExpected;
        context.error(DiagnosticCodes.UnexpectedInPlacedFile,
            `Unexpected '${word}' in a file imported into ${describePlacement(placement)} - expected an application declaration or ${expected}`, locationOf(line));
    } else {
        const hint = bodyKeywords.has(word)
            ? ` - '${word}' belongs in a module or feature; wrap it in one, or import this file from inside one`
            : ' - expected domain, import, concept, type, policy, persona, sticky note, authentication, onboarding, identity, context, module, seed, trigger, behavior, ui profile, design system, theme, sample data, layout, statemachine or workflow';
        context.error(DiagnosticCodes.UnknownTopLevelConstruct, `Unexpected '${word}' at the top level${hint}`, locationOf(line));
    }
    context.skipBlock(line.indent);
}

// Wraps the feature a file is placed in in the features and module around it, each marked as a placement.
function place(placement: PlayPlacement, innermost: FeatureSyntax, start: SourceLocation): ModuleSyntax {
    let feature = innermost;
    for (let index = placement.length - 2; index >= 1; index--) {
        feature = {
            kind: 'FeatureSyntax', name: placement[index], description: null, authorize: null,
            features: [feature], slices: [], fileImports: [], isPlacement: true, location: start,
        };
    }
    return {
        kind: 'ModuleSyntax', name: placement[0], description: null, authorize: null,
        features: [feature], fileImports: [], isPlacement: true, location: start,
    };
}

function parseDomain(context: ParserContext, line: SourceLine, existing: DomainSyntax | null, sawOtherConstruct: boolean): DomainSyntax | null {
    const match = domainPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDomainDeclaration, `Invalid domain declaration '${line.content}' - expected 'domain <Qualified.Name>'`, locationOf(line));
        return existing;
    }
    if (existing !== null) {
        context.error(DiagnosticCodes.DuplicateDomain, 'The document already declares a domain - a document can have at most one', locationOf(line));
        return existing;
    }
    if (sawOtherConstruct) {
        context.error(DiagnosticCodes.DomainNotFirst, '\'domain\' must be declared before any other construct', locationOf(line));
    }
    return { kind: 'DomainSyntax', name: match[1], location: locationOf(line) };
}

function warnOnTabIndentation(context: ParserContext, lines: readonly SourceLine[]): void {
    let inFence = false;
    for (const line of lines) {
        if (line.raw.trimStart().startsWith('```')) {
            inFence = !inFence;
            continue;
        }
        if (!inFence && tabIndentPattern.test(line.raw)) {
            context.warning(DiagnosticCodes.TabIndentation, 'Screenplay is indentation based - use spaces, not tabs', startOf(line));
        }
    }
}

function parseConcept(context: ParserContext, line: SourceLine): ConceptSyntax {
    const match = conceptPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidConceptDeclaration, `Invalid concept declaration '${line.content}' - expected 'concept <Name> : <Type>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ConceptSyntax', name: firstWord(line.content), type: '', attributes: [], values: [], location: locationOf(line) };
    }
    const [, name, type, attributeText] = match;
    const attributes = parseComplianceMarkers(context, line, name, attributeText);
    if (type !== 'Enum' && !primitiveTypes.includes(type)) {
        context.error(DiagnosticCodes.UnknownPrimitiveType, `Unknown primitive type '${type}' - expected ${primitiveTypes.join(', ')} or Enum`, locationOf(line));
    }
    const values: string[] = [];
    const members = new EnumerationMembers();
    let generation: GenerationSyntax | null = null;
    let declaredGeneration = false;
    let description: string | null = null;
    const validations: ValidateSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const memberName = type === 'Enum' ? members.tryNumbered(context, child) : undefined;
        if (memberName !== undefined) {
            values.push(memberName);
        } else if (firstWord(child.content) === 'description' && (type !== 'Enum' || child.content !== 'description' || context.peekChild(child.indent)?.content.startsWith('```') === true)) {
            description = parseDescription(context, child, description, `Concept '${name}'`);
        } else if (isFileDirective(child)) {
            continue;
        } else if (firstWord(child.content) === 'validate') {
            if (type === 'Enum' && child.content === 'validate' && context.peekChild(child.indent) === undefined) {
                context.warning(DiagnosticCodes.ValidateReadAsEnumerationBlock,
                    `'validate' in enumeration concept '${name}' declares an empty validate block, not a value named 'validate' - write '@validate' for the value`,
                    locationOf(child));
            }
            const parsed = parseValidate(context, child, 'concept', name);
            if (parsed !== undefined) validations.push(parsed);
        } else if (firstWord(child.content) === 'generate' && !(type === 'Enum' && child.content === 'generate')) {
            if (declaredGeneration) context.error(DiagnosticCodes.DuplicateGeneration, `Concept '${name}' declares generation more than once.`, locationOf(child));
            const parsed = parseGeneration(context, child, type);
            if (!declaredGeneration) generation = parsed;
            declaredGeneration = true;
        } else if (parseComplianceDirective(context, child, name, attributes)) {
            // Compliance directives are modeled by their original concept marker.
        } else if (type === 'Enum' && enumValuePattern.test(child.content)) {
            values.push(unescapeIdentifier(child.content));
        } else if (type === 'Enum') {
            context.error(DiagnosticCodes.InvalidEnumerationValue, `Invalid enum value '${child.content}' - expected an identifier`, locationOf(child));
        } else {
            context.error(DiagnosticCodes.UnknownConceptDirective, `Unexpected '${child.content}' in concept body - expected validate, generate, 'file <path>' or '<attribute> reason "<text>"'${misplacedRuleHintForConcept(child.content)}`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    const valueNumbers = members.finish(context, line, values.length);
    return { kind: 'ConceptSyntax', name, type, attributes, values, validations, description, ...(generation === null ? {} : { generation }), ...(valueNumbers === undefined ? {} : { valueNumbers }), location: locationOf(line) };
}

// 'user <name> ["<display name>"] [as <Persona>, ...]' - the port of the C# ParseUser.
function parseUser(context: ParserContext, line: SourceLine): UserSyntax {
    const match = userPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidUserDeclaration, `Invalid user declaration '${line.content}' - expected 'user <name> ["<display name>"] [as <Persona>, ...]'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'UserSyntax', name: firstWord(line.content), displayName: null, personas: [], location: locationOf(line) };
    }
    // A user is one line: its body would have nothing to say, so a nested line is reported instead of silently dropped.
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidUserDeclaration, `A user is declared on one line; '${child.content}' has no place under it`, locationOf(child));
        context.skipBlock(line.indent);
    }
    return {
        kind: 'UserSyntax',
        name: match[1],
        displayName: match[2] === undefined ? null : unescapeString(match[2]),
        personas: match[3] === undefined ? [] : match[3].split(',').map(name => name.trim()).filter(name => name !== ''),
        location: locationOf(line),
    };
}

// 'persona <Name>' with an optional description and the policies it holds - the port of the C# ParsePersona.
function parsePersona(context: ParserContext, line: SourceLine): PersonaSyntax {
    const name = personaPattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid persona declaration '${line.content}' - expected 'persona <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    const policies: string[] = [];
    const goals: string[] = [];
    let position: readonly [number, number] | null = null;
    let size: readonly [number, number] | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Persona '${name}'`);
        } else if (keyword === 'goal') {
            const goal = personaGoalPattern.exec(child.content);
            if (goal === null) {
                context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid goal '${child.content}' - expected 'goal "<text>"'`, locationOf(child));
            } else {
                goals.push(unescapeString(goal[1]));
            }
        } else if (keyword === 'position') {
            position = parseCanvasPair(context, child, 'position', 'x', 'y', position, true) ?? position;
        } else if (keyword === 'size') {
            size = parseCanvasPair(context, child, 'size', 'width', 'height', size, false) ?? size;
        } else if (keyword === 'policy') {
            const policy = personaPolicyPattern.exec(child.content);
            if (policy === null) {
                context.error(DiagnosticCodes.InvalidPersonaPolicyReference, `Invalid policy reference '${child.content}' - expected 'policy <Name>'`, locationOf(child));
            } else {
                policies.push(policy[1]);
            }
        } else {
            context.error(DiagnosticCodes.UnknownPersonaDirective, `Unexpected '${keyword}' in persona body - expected description, goal, policy, position or size`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return {
        kind: 'PersonaSyntax', name, description, policies,
        ...(goals.length > 0 ? { goals } : {}),
        ...(position === null ? {} : { x: position[0], y: position[1] }),
        ...(size === null ? {} : { width: size[0], height: size[1] }),
        location: locationOf(line),
    };
}

// 'sticky note on <canvas>' with the text it carries, an optional colour and where it sits on that canvas - the port of the C# ParseStickyNote.
function parseStickyNote(context: ParserContext, line: SourceLine): StickyNoteSyntax | null {
    const canvasMatch = stickyNotePattern.exec(line.content);
    if (canvasMatch === null) {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid sticky note declaration '${line.content}' - expected 'sticky note on personas', 'sticky note on eventsources', 'sticky note on givencontexts' or 'sticky note on seeddata'`, locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }
    let text: string | null = null;
    let colour: string | null = null;
    let position: readonly [number, number] | null = null;
    let size: readonly [number, number] | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            text = parseDescription(context, child, text, 'A sticky note');
        } else if (keyword === 'colour') {
            const match = stickyNoteColourPattern.exec(child.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid colour '${child.content}' - expected 'colour yellow', 'colour pink', 'colour green' or 'colour blue'`, locationOf(child));
            } else {
                colour = match[1];
            }
        } else if (keyword === 'position') {
            position = parseCanvasPair(context, child, 'position', 'x', 'y', position, true) ?? position;
        } else if (keyword === 'size') {
            size = parseCanvasPair(context, child, 'size', 'width', 'height', size, false) ?? size;
        } else {
            context.error(DiagnosticCodes.UnknownPersonaDirective, `Unexpected '${keyword}' in sticky note body - expected description, colour, position or size`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    if (text === null || position === null) {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, 'A sticky note needs a description (the text on it) and a position', locationOf(line));
        return null;
    }
    return { kind: 'StickyNoteSyntax', canvas: canvasMatch[1] as StickyNoteSyntax['canvas'], text, colour, x: position[0], y: position[1], width: size === null ? null : size[0], height: size === null ? null : size[1], location: locationOf(line) };
}

