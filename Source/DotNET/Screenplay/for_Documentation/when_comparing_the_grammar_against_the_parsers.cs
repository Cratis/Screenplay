// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.for_Documentation;

public partial class when_comparing_the_grammar_against_the_parsers : Specification
{
    // The key is the parser's case label, not a second list of constructs. Each entry points to the
    // EBNF production and reference page that must describe that accepted surface form.
    static readonly IReadOnlyDictionary<string, (string Production, string Page)> _references =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["authentication"] = ("AuthenticationDecl", "authentication.md"),
            ["behavior"] = ("BehaviorDecl", "interactions.md"),
            ["capture"] = ("CaptureDecl", "captures.md"),
            ["command"] = ("CommandDecl", "commands.md"),
            ["concept"] = ("ConceptDecl", "concepts.md"),
            ["constraint"] = ("ConstraintDecl", "constraints.md"),
            ["contribute"] = ("ContributionDecl", "contributions.md"),
            ["description"] = ("DescriptionDecl", "slices.md"),
            ["dialog"] = ("DialogTemplateDecl", "templates.md"),
            ["domain"] = ("DomainDecl", "domain.md"),
            ["event"] = ("EventDecl", "events.md"),
            ["feature"] = ("Feature", "slices.md"),
            ["form"] = ("FormDecl", "forms.md"),
            ["import"] = ("Import", "domain.md"),
            ["layout"] = ("LayoutDecl", "templates.md"),
            ["module"] = ("Module", "slices.md"),
            ["on"] = ("InteractionBinding", "interactions.md"),
            ["persona"] = ("PersonaDecl", "personas.md"),
            ["policy"] = ("PolicyDecl", "policies.md"),
            ["projection"] = ("ProjectionDecl", "projections/index.md"),
            ["query"] = ("QueryDecl", "queries.md"),
            ["reaction"] = ("ReactionDecl", "reactions.md"),
            ["readmodel"] = ("ReadModelDecl", "readmodels.md"),
            ["reducer"] = ("ReducerDecl", "readmodels.md"),
            ["screen"] = ("ScreenDecl", "screens.md"),
            ["seed"] = ("SeedDecl", "seeding.md"),
            ["slice"] = ("SliceDecl", "slices.md"),
            ["specification"] = ("SpecificationDecl", "specifications.md"),
            ["theme"] = ("ThemeDecl", "theme.md"),
            ["trigger"] = ("TriggerDecl", "triggers.md"),
            ["type"] = ("TypeDecl", "types.md"),
            ["ui"] = ("UiProfileDecl", "ui-profile.md"),
            ["uses"] = ("UsesBehaviorDecl", "interactions.md")
        };

    HashSet<string> _dispatched;
    List<string> _missing;

    void Establish()
    {
        var parsing = Path.Combine(Directory.GetParent(DocumentationExamples.Root())!.FullName, "Source", "DotNET", "Screenplay", "Parsing");
        var document = File.ReadAllText(Path.Combine(parsing, "ScreenplayParser.cs"));
        var slices = File.ReadAllText(Path.Combine(parsing, "SliceParser.cs"));
        _dispatched = [];
        AddCases(document, "switch (LineText.FirstWord(line.Content))", "static void AddLayout", "document", _dispatched);
        AddCases(document, "static ModuleSyntax ParseModule", "static void AddForm", "module", _dispatched);
        AddCases(document, "static FeatureSyntax ParseFeature", "[GeneratedRegex", "feature", _dispatched);
        AddCases(slices, "switch (LineText.FirstWord(line.Content))", "return new(type, name", "slice", _dispatched);
    }

    void Because()
    {
        var root = Path.Combine(DocumentationExamples.Root(), "screenplay");
        var grammar = File.ReadAllText(Path.Combine(root, "grammar.md"));
        _missing = [];
        foreach (var dispatch in _dispatched.Order(StringComparer.Ordinal))
        {
            var keyword = dispatch[(dispatch.IndexOf(':') + 1)..];
            if (!_references.TryGetValue(keyword, out var reference))
            {
                _missing.Add($"{dispatch}: no reference mapping");
                continue;
            }

            // "screen" means a template in a module but a concrete screen in a slice.
            if (dispatch == "module:screen")
            {
                reference = ("ScreenTemplateDecl", "templates.md");
            }

            if (!Regex.IsMatch(grammar, $"^{Regex.Escape(reference.Production)}\\s*=", RegexOptions.Multiline, TimeSpan.FromSeconds(1)))
            {
                _missing.Add($"{dispatch}: missing {reference.Production} production in grammar.md");
            }

            var path = Path.Combine(root, reference.Page);
            if (!File.Exists(path) || !Regex.IsMatch(File.ReadAllText(path), $@"(?<![\w]){Regex.Escape(keyword)}(?![\w])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            {
                _missing.Add($"{dispatch}: missing reference in {reference.Page}");
            }
        }
    }

    [Fact] void should_find_dispatched_constructs() => _dispatched.Count.ShouldBeGreaterThan(38);
    [Fact] void should_document_every_dispatched_construct() => string.Join('\n', _missing).ShouldEqual(string.Empty);

    static void AddCases(string source, string start, string end, string scope, HashSet<string> keywords)
    {
        var first = source.IndexOf(start, StringComparison.Ordinal);
        var last = source.IndexOf(end, first + start.Length, StringComparison.Ordinal);
        if (first < 0 || last < 0)
        {
            return; // The non-vacuity assertion catches a changed/missing dispatch shape.
        }

        foreach (Match match in CaseLabelRegex().Matches(source[first..last]))
        {
            keywords.Add($"{scope}:{match.Groups[1].Value}");
        }
    }

    [GeneratedRegex("case \"([a-z]+)\":", RegexOptions.None, 1000)]
    private static partial Regex CaseLabelRegex();
}
