// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayParser
{
    /// <summary>
    /// The root rules in the order they are tried: the first one whose keyword matches and whose guard holds handles
    /// the line, so a keyword that has a guarded rule keeps that rule ahead of its unguarded one. A line no rule
    /// takes goes to the placed body, then to the unexpected top level report.
    /// </summary>
    static readonly RootRule[] _rootRules =
    [
        new("command", (line, _) => CommandReadsParser.IsDeclaration(line.Content), ParseRootCommandReads),
        new("time", (line, _) => TimeZoneParser.IsDeclaration(line.Content), ParseRootTimeZone),
        new("reaction", null, ParseRootReactionBatches),
        new("domain", null, ParseRootDomain),
        new("import", (line, _) => FileImportParser.IsFileImport(line.Content), ParseRootFileImport),
        new("import", null, ParseRootImport),
        new("example", null, ParseRootExample),
        new("eventsource", null, (context, line, state) => state.EventSources.Add(EventSourceParser.Parse(context, line))),
        new("system", null, (context, line, state) => state.Systems.Add(OperationParser.ParseSystem(context, line))),
        new("statemachine", null, (context, line, state) => state.StateMachines.Add(StateMachineParser.Parse(context, line))),
        new("workflow", null, (context, line, state) => state.Workflows.Add(WorkflowParser.Parse(context, line))),
        new("concept", null, (context, line, state) => state.Concepts.Add(ParseConcept(context, line))),
        new("type", null, (context, line, state) => state.Types.Add(TypeParser.Parse(context, line))),
        new("purpose", (_, state) => state.HasPlacedBody, ParseRootPlacedLine),
        new("purpose", null, (context, line, state) => state.Purposes.Add(PurposeParser.Parse(context, line))),
        new("policy", null, (context, line, state) => state.Policies.Add(PolicyParser.Parse(context, line))),
        new("persona", null, (context, line, state) => state.Personas.Add(ParsePersona(context, line))),
        new("user", null, (context, line, state) => state.Users.Add(ParseUser(context, line))),
        new("storyline", null, (context, line, state) => state.Storylines.Add(StorylineParser.Parse(context, line))),
        new("sink", null, (context, line, state) => { if (SinkParser.ParseDeclaration(context, line) is { } sink) state.Sinks.Add(sink); }),
        new("sticky", null, ParseRootStickyNote),
        new("context", null, (context, line, state) => state.ApplicationContext = ApplicationContextParser.Parse(context, line, state.ApplicationContext, state.Placement)),
        new("identity", null, (context, line, state) => state.Identity = IdentityParser.Parse(context, line, state.Identity)),
        new("authentication", null, (context, line, state) => state.Authentication = AuthenticationParser.Parse(context, line, state.Authentication)),
        new("onboarding", null, (context, line, state) => state.Onboarding = OnboardingParser.Parse(context, line, state.Onboarding)),
        new("module", (_, state) => !state.Placement.IsDocument, (context, line, state) => ParseModuleInPlacedFile(context, line, state.Placement, state.ModuleBody)),
        new("module", null, (context, line, state) => state.Modules.Add(ParseModule(context, line))),
        new("seed", null, (context, line, state) => state.Seeds.Add(SeedParser.Parse(context, line))),
        new("ui", null, (context, line, state) => AddUiProfile(context, UiProfileParser.Parse(context, line), state.UiProfiles)),
        new("sample", null, (context, line, state) => AddSampleDataPack(context, SampleDataPackParser.Parse(context, line), state.SampleDataPacks)),
        new("design", null, (context, line, state) => AddDesignSystem(context, DesignSystemParser.Parse(context, line), state.DesignSystems)),
        new("theme", null, (context, line, state) => AddTheme(context, ThemeParser.Parse(context, line), state.Themes)),
        new("trigger", null, (context, line, state) => AddTrigger(context, TriggerParser.Parse(context, line), state.Triggers)),
        new("layout", null, (context, line, state) => AddLayout(context, LayoutParser.ParseLayout(context, line), state.Layouts)),
        new("template", null, ParseRootTemplate),
        new("behavior", null, (context, line, state) => AddBehavior(context, InteractionParser.ParseBehavior(context, line), state.Behaviors)),
        new("exposure", null, (context, line, state) => state.Exposures.Add(CompositionParser.ParseExposure(context, line))),
        new("instance", null, (context, line, state) => state.InstanceContributions.Add(CompositionParser.ParseInstance(context, line))),
        new("runs", null, (context, line, _) => ReactionIdentityParser.Misplaced(context, line))
    ];

    static void DispatchRoot(ParserContext context, SourceLine line, RootBodyState state)
    {
        var keyword = LineText.FirstWord(line.Content);
        foreach (var rule in _rootRules)
        {
            if (rule.Matches(keyword, line, state))
            {
                rule.Parse(context, line, state);
                return;
            }
        }

        if (state.ModuleBody?.TryParse(context, line) == true || state.FeatureBody?.TryParse(context, line) == true)
        {
            return;
        }

        ReportUnexpectedTopLevel(context, line, state.Placement);
    }

    static void ParseRootCommandReads(ParserContext context, SourceLine line, RootBodyState state)
    {
        var refused = IsRepeatedOrMisplaced(state.SawCommandReads, state) || !CommandReadsParser.IsProtected(line.Content);
        if (DeclareOnce(context, line, refused, DiagnosticCodes.InvalidCommandReadsDeclaration, "Declare 'command reads protected' once at the application root."))
        {
            state.CommandReadMode = CommandReadMode.Protected;
        }

        state.SawCommandReads = true;
    }

    static void ParseRootTimeZone(ParserContext context, SourceLine line, RootBodyState state)
    {
        if (IsRepeatedOrMisplaced(state.SawTimeZone, state) || TimeZoneParser.Parse(context, line) is not { } declared)
        {
            context.Error(DiagnosticCodes.InvalidTimeZoneDeclaration, "Declare 'time zone \"<id>\"' or 'time zone configurable default \"<id>\"' once at the application root.", line.Location);
        }
        else
        {
            state.TimeZone = declared;
        }

        state.SawTimeZone = true;
    }

    static void ParseRootReactionBatches(ParserContext context, SourceLine line, RootBodyState state)
    {
        var refused = IsRepeatedOrMisplaced(state.SawReactionBatches, state) || !ReactionBatchesParser.IsAtomic(line.Content);
        if (DeclareOnce(context, line, refused, DiagnosticCodes.InvalidReactionBatchesDeclaration, "Declare 'reaction batches atomic' once at the application root."))
        {
            state.ReactionProductionMode = ReactionProductionMode.Atomic;
        }

        state.SawReactionBatches = true;
    }

    static bool IsRepeatedOrMisplaced(bool alreadyDeclared, RootBodyState state) => alreadyDeclared || !state.Placement.IsDocument;

    static bool DeclareOnce(ParserContext context, SourceLine line, bool refused, string code, string message)
    {
        if (refused)
        {
            context.Error(code, message, line.Location);
        }

        return !refused;
    }

    static void ParseRootDomain(ParserContext context, SourceLine line, RootBodyState state) =>
        state.Domain = ParseDomain(context, line, state.Domain, state.DeclaredBeforeDomain);

    static void ParseRootFileImport(ParserContext context, SourceLine line, RootBodyState state)
    {
        // A top level import belongs to whatever the document's top level is - the application, or the
        // module or feature the document was itself imported into.
        if (state.HasPlacedBody)
        {
            _ = state.ModuleBody?.TryParse(context, line) ?? state.FeatureBody!.TryParse(context, line);
        }
        else
        {
            FileImportParser.Parse(context, line, state.FileImports);
        }
    }

    static void ParseRootImport(ParserContext context, SourceLine line, RootBodyState state)
    {
        if (ImportRegex().Match(line.Content) is not { Success: true } import)
        {
            context.Error(DiagnosticCodes.InvalidImportDeclaration, $"Invalid import '{line.Content}' - expected 'import <Qualified.Name> [from \"<origin>\"]'", line.Location);
            return;
        }

        var origin = import.Groups["origin"].Success ? StringLiteral.Unescape(import.Groups["origin"].Value) : null;
        if (origin is not null && string.IsNullOrWhiteSpace(origin))
        {
            context.Error(DiagnosticCodes.InvalidImportDeclaration, "An import origin must be a nonblank quoted string", line.Location);
        }

        state.Imports.Add(new(import.Groups[1].Value, line.Location)
        {
            Origin = origin,
            Visibility = origin is null ? EventVisibility.Private : EventVisibility.Public
        });
    }

    static void ParseRootExample(ParserContext context, SourceLine line, RootBodyState state)
    {
        if (state.HasPlacedBody)
        {
            _ = state.ModuleBody?.TryParse(context, line) ?? state.FeatureBody!.TryParse(context, line);
        }
        else
        {
            state.Examples.Add(SpecificationParser.ParseExample(context, line));
        }
    }

    static void ParseRootPlacedLine(ParserContext context, SourceLine line, RootBodyState state) =>
        _ = state.ModuleBody?.TryParse(context, line) ?? state.FeatureBody!.TryParse(context, line);

    static void ParseRootStickyNote(ParserContext context, SourceLine line, RootBodyState state)
    {
        if (ParseStickyNote(context, line) is { } stickyNote)
        {
            state.StickyNotes.Add(stickyNote);
        }
    }

    static void ParseRootTemplate(ParserContext context, SourceLine line, RootBodyState state)
    {
        if (TemplateAssignmentParser.Parse(context, line) is { } template)
        {
            state.Templates.Add(template);
        }
    }

    /// <summary>
    /// A root rule: the keyword that opens it, an optional guard that must also hold, and what to do with the line.
    /// </summary>
    /// <param name="Keyword">The first word of the line the rule takes.</param>
    /// <param name="When">An extra condition on the line and the state, or <see langword="null"/> when the keyword alone decides.</param>
    /// <param name="Parse">Handles the line.</param>
    sealed record RootRule(string Keyword, Func<SourceLine, RootBodyState, bool>? When, Action<ParserContext, SourceLine, RootBodyState> Parse)
    {
        public bool Matches(string keyword, SourceLine line, RootBodyState state) =>
            string.Equals(keyword, Keyword, StringComparison.Ordinal) && (When?.Invoke(line, state) ?? true);
    }

    /// <summary>
    /// Everything the root loop gathers while it reads the lines of a document.
    /// </summary>
    /// <param name="placement">Where the document's top level belongs.</param>
    sealed class RootBodyState(PlayPlacement placement)
    {
        public PlayPlacement Placement { get; } = placement;

        public ModuleBody? ModuleBody { get; } = placement.Scope.Count == 1 ? new ModuleBody(placement.Scope[0]) : null;

        public FeatureBody? FeatureBody { get; } = placement.Scope.Count > 1 ? new FeatureBody(placement.Scope[^1]) : null;

        public bool HasPlacedBody => ModuleBody is not null || FeatureBody is not null;

        public List<FileImportSyntax> FileImports { get; } = [];

        public DomainSyntax? Domain { get; set; }

        public AuthenticationSyntax? Authentication { get; set; }

        public IdentitySyntax? Identity { get; set; }

        public OnboardingSyntax? Onboarding { get; set; }

        public ReactionProductionMode? ReactionProductionMode { get; set; }

        public bool SawReactionBatches { get; set; }

        public CommandReadMode? CommandReadMode { get; set; }

        public bool SawCommandReads { get; set; }

        public TimeZoneSyntax? TimeZone { get; set; }

        public ApplicationContextSyntax? ApplicationContext { get; set; }

        public bool SawTimeZone { get; set; }

        public List<ImportSyntax> Imports { get; } = [];

        public List<ConceptSyntax> Concepts { get; } = [];

        public List<TypeSyntax> Types { get; } = [];

        public List<PolicySyntax> Policies { get; } = [];

        public List<PurposeSyntax> Purposes { get; } = [];

        public List<PersonaSyntax> Personas { get; } = [];

        public List<UserSyntax> Users { get; } = [];

        public List<StorylineSyntax> Storylines { get; } = [];

        public List<SinkSyntax> Sinks { get; } = [];

        public List<StickyNoteSyntax> StickyNotes { get; } = [];

        public List<ModuleSyntax> Modules { get; } = [];

        public List<SeedSyntax> Seeds { get; } = [];

        public List<UiProfileSyntax> UiProfiles { get; } = [];

        public List<ThemeSyntax> Themes { get; } = [];

        public List<DesignSystemSyntax> DesignSystems { get; } = [];

        public List<SampleDataPackSyntax> SampleDataPacks { get; } = [];

        public List<TriggerSyntax> Triggers { get; } = [];

        public List<LayoutSyntax> Layouts { get; } = [];

        public List<BehaviorSyntax> Behaviors { get; } = [];

        public List<SystemSyntax> Systems { get; } = [];

        public List<StateMachineSyntax> StateMachines { get; } = [];

        public List<WorkflowSyntax> Workflows { get; } = [];

        public List<EventSourceSyntax> EventSources { get; } = [];

        public List<SpecificationExampleSyntax> Examples { get; } = [];

        public List<TemplateAssignmentSyntax> Templates { get; } = [];

        public List<ExposureSyntax> Exposures { get; } = [];

        public List<InstanceContributionsSyntax> InstanceContributions { get; } = [];

        /// <summary>
        /// Gets a value indicating whether any construct the domain must precede was declared before it.
        /// </summary>
        public bool DeclaredBeforeDomain =>
            Imports.Count > 0 || Concepts.Count > 0 || Types.Count > 0 || Policies.Count > 0 || Purposes.Count > 0 || Personas.Count > 0 || Users.Count > 0 || Storylines.Count > 0 || Sinks.Count > 0 || Modules.Count > 0 || Seeds.Count > 0 || Authentication is not null || Identity is not null || ApplicationContext is not null || Onboarding is not null || UiProfiles.Count > 0 || Themes.Count > 0 || DesignSystems.Count > 0 || Triggers.Count > 0 || Layouts.Count > 0 || Systems.Count > 0 || EventSources.Count > 0 || Examples.Count > 0;

        /// <summary>
        /// Builds the <see cref="ApplicationSyntax"/>, placing the module or feature a placed document opened.
        /// </summary>
        /// <param name="context">The <see cref="ParserContext"/> the document was parsed in.</param>
        /// <returns>The parsed <see cref="ApplicationSyntax"/>.</returns>
        public ApplicationSyntax Build(ParserContext context)
        {
            if (ModuleBody is not null)
            {
                Modules.Insert(0, ModuleBody.Build(context.Start, isPlacement: true));
            }
            else if (FeatureBody is not null)
            {
                Modules.Insert(0, Place(Placement, FeatureBody.Build(context.Start, isPlacement: true), context.Start));
            }

            return new(Imports, Concepts, Policies, Modules, context.Start, Domain, Personas, Seeds, Authentication, Types, UiProfiles, Themes, Triggers, Layouts)
            {
                Identity = Identity,
                DesignSystems = DesignSystems,
                Onboarding = Onboarding,
                Users = Users,
                Storylines = Storylines,
                Sinks = Sinks,
                ReactionProductionMode = ReactionProductionMode,
                CommandReadMode = CommandReadMode,
                TimeZone = TimeZone,
                Context = ApplicationContext,
                SourceOptions = context.SourceOptions,
                Purposes = Purposes,
                StickyNotes = StickyNotes,
                Examples = Examples,
                Systems = Systems,
                StateMachines = StateMachines,
                Workflows = Workflows,
                SampleDataPacks = SampleDataPacks,
                EventSources = EventSources,
                Behaviors = Behaviors,
                FileImports = FileImports,
                Templates = Templates,
                Exposures = Exposures,
                InstanceContributions = InstanceContributions
            };
        }
    }
}
