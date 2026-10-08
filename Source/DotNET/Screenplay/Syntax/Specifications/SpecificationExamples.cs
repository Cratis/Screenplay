// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Resolves typed examples and exposes complete effective steps to syntax consumers.
/// </summary>
public static class SpecificationExamples
{
    /// <summary>
    /// Expands specification fixtures without mutating authored nodes or introducing defaults.
    /// </summary>
    /// <param name="application">The complete authored application and its declaration scopes.</param>
    /// <returns>The effective syntax, value provenance, and resolution diagnostics.</returns>
    public static EffectiveSpecificationApplication Expand(ApplicationSyntax application) => new Expansion(application).Expand();

    /// <summary>
    /// Expands a standalone specification against its owning application's declarations.
    /// </summary>
    /// <param name="specification">The standalone specification, including its document-scoped examples.</param>
    /// <param name="declarations">The application declaring the fixture types and shared examples.</param>
    /// <param name="scope">The module, nested features, and slice of the specification's use site.</param>
    /// <returns>The effective specification and any resolution diagnostics.</returns>
    public static CompilationResult<EffectiveSpecification> Expand(SpecificationSyntax specification, ApplicationSyntax declarations, IReadOnlyList<string> scope) =>
        new Expansion(declarations, specification.Examples, new(scope)).ExpandStandalone(specification, new(scope));

    sealed class Expansion
    {
        readonly ApplicationSyntax _application;
        readonly List<Entry> _entries = [];
        readonly List<(SliceSyntax Slice, DeclarationScope Scope)> _slices = [];
        readonly List<EffectiveSpecification> _specifications = [];
        readonly ParserContext _context = ParserContext.ForDiagnostics();
        readonly Dictionary<SpecificationExampleSyntax, Entry> _types = new(ReferenceEqualityComparer.Instance);
        readonly ReferenceDeclarationIndex _index;
        readonly ConsistencyDeclarations? _declarations;

        internal Expansion(ApplicationSyntax application, IEnumerable<SpecificationExampleSyntax>? standaloneExamples = null, DeclarationScope? standaloneScope = null)
        {
            _application = application;
            AddExamples(application.Examples, new([]));
            foreach (var concept in application.Concepts) Add(concept.Name, new([]), concept, "concept", []);
            foreach (var type in application.Types ?? []) Add(type.Name, new([]), type, "type", type.Properties);
            foreach (var module in application.Modules)
            {
                AddExamples(module.Examples, new([module.Name]));
                IndexFeatures(module.Features, [module.Name]);
            }

            if (standaloneExamples is not null)
            {
                var examples = standaloneExamples.ToArray();
                _entries.RemoveAll(entry => examples.Any(example => ReferenceEquals(example, entry.Node)));
                AddExamples(examples, standaloneScope!);
            }

            _index = new(_entries.Select(entry => entry.Declaration));
            if (_entries.Exists(entry => entry.Kind == "example")) _declarations = new(application, _slices);
        }

        internal EffectiveSpecificationApplication Expand()
        {
            foreach (var entry in _entries.Where(entry => entry.Node is SpecificationExampleSyntax)) ValidateExample(entry);
            var effective = _application with
            {
                Modules = [.. _application.Modules.Select(module => module with { Features = [.. module.Features.Select(ExpandFeature)] })]
            };

            return new(effective, [.. _specifications], [.. _context.Diagnostics.Distinct()])
            {
                ResolvedExamples = [.. _types.Select(entry => new ResolvedSpecificationExample(entry.Key, entry.Value.Kind, entry.Value.Node, entry.Value.Properties))]
            };
        }

        internal CompilationResult<EffectiveSpecification> ExpandStandalone(SpecificationSyntax specification, DeclarationScope scope)
        {
            foreach (var entry in _entries.Where(entry => entry.Node is SpecificationExampleSyntax)) ValidateExample(entry);
            ExpandSpecification(specification, scope);

            return new(_specifications[0], _context.Diagnostics);
        }

        static string Qualified(Entry type) => string.Join('.', type.Declaration.Scope.Segments.Append(type.Declaration.Name));

        static PropertyMappingSyntax[] Merge(IEnumerable<PropertyMappingSyntax> inherited, IEnumerable<PropertyMappingSyntax> authored) =>
            [.. inherited.Where(value => !authored.Any(replacement => replacement.Property == value.Property)), .. authored];

        static IEnumerable<EffectiveSpecificationValue> Origins(IEnumerable<PropertyMappingSyntax> inherited, IEnumerable<PropertyMappingSyntax> authored, string prefix = "")
        {
            foreach (var value in Merge(inherited, authored))
            {
                var replacement = authored.Any(item => ReferenceEquals(item, value));
                var prior = inherited.FirstOrDefault(item => item.Property == value.Property);
                var origin = (replacement, prior) switch
                {
                    (false, _) => SpecificationValueOrigin.Example,
                    (true, null) => SpecificationValueOrigin.Authored,
                    _ => SpecificationValueOrigin.Override
                };
                yield return new(prefix + value.Property, value.Source, origin, replacement ? prior?.Source : null);
            }
        }

        static IEnumerable<EffectiveSpecificationValue> ForOrigin(ExpressionSyntax? inherited, ExpressionSyntax? authored)
        {
            var origin = (authored, inherited) switch
            {
                (null, _) => SpecificationValueOrigin.Example,
                (_, null) => SpecificationValueOrigin.Authored,
                _ => SpecificationValueOrigin.Override
            };

            return (authored ?? inherited) is { } value ? [new("for", value, origin, authored is null ? null : inherited)] : [];
        }

        static string CorrectedRole(string kind, string role) => (kind, role.Split(' ')[0]) switch
        {
            ("command", _) => "when",
            ("readmodel", "given") => "given readmodel",
            ("readmodel", _) => "then readmodel",
            (_, "when") => "when append",
            (_, var keyword) => keyword
        };

        void Add(string name, DeclarationScope scope, SyntaxNode node, string kind, IEnumerable<PropertySyntax> properties) =>
            _entries.Add(new(new(name, scope), node, kind, properties));

        void AddExamples(IEnumerable<SpecificationExampleSyntax> examples, DeclarationScope scope)
        {
            foreach (var example in examples) Add(example.Name, scope, example, "example", []);
        }

        void IndexFeatures(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path)
        {
            foreach (var feature in features)
            {
                var featurePath = path.Concat([feature.Name]).ToArray();
                AddExamples(feature.Examples, new(featurePath));
                foreach (var slice in feature.Slices)
                {
                    var scope = new DeclarationScope([.. featurePath, slice.Name]);
                    _slices.Add((slice, scope));
                    AddExamples(slice.Examples, scope);
                    foreach (var group in EventDeclarations.In(slice).GroupBy(node => node.Name, StringComparer.Ordinal))
                    {
                        var current = group.OrderByDescending(node => node.Generation).First();
                        Add(current.Name, scope, current, "event", current.Properties);
                    }

                    foreach (var command in slice.Commands) Add(command.Name, scope, command, "command", command.Properties);
                    foreach (var model in slice.ReadModels ?? []) Add(model.Name, scope, model, "readmodel", model.Properties);
                }

                IndexFeatures(feature.Features, featurePath);
            }
        }

        Entry? Resolve(string name, DeclarationScope scope)
        {
            var resolution = ReferenceResolver.Resolve(name, scope, _index);
            if (resolution.IsUnresolved && _application.Imports.Where(import => import.Name == name).ToArray() is [var imported])
            {
                resolution = ReferenceResolver.Resolve(imported.QualifiedName, scope, _index);
            }

            return resolution.Resolved is { } declaration ? _entries.First(entry => ReferenceEquals(entry.Declaration, declaration)) : null;
        }

        void ValidateExample(Entry entry)
        {
            var example = (SpecificationExampleSyntax)entry.Node;
            if (_entries.Exists(other => other.Kind != "example" && other.Declaration.Name == example.Name) ||
                _application.Imports.Any(import => import.Name == example.Name) || ConceptSyntax.PrimitiveTypes.Contains(example.Name))
            {
                _context.Error(DiagnosticCodes.SpecificationExampleNameCollision, $"Example '{example.Name}' collides with a type name; choose a distinct example name.", example.Location);
            }

            if (_entries.Count(other => other.Kind == "example" && other.Declaration == entry.Declaration) > 1)
            {
                _context.Error(DiagnosticCodes.SpecificationExampleNameCollision, $"Example '{example.Name}' is declared more than once in the same scope.", example.Location);
            }

            var type = Resolve(example.Type, entry.Declaration.Scope);
            if (type is null)
            {
                _context.Error(DiagnosticCodes.UnresolvedSpecificationExampleType, $"Example '{example.Name}' type '{example.Type}' is unknown or ambiguous; qualify a declared event, command, or read model.", example.Location);
                return;
            }

            if (type.Kind is not ("event" or "command" or "readmodel"))
            {
                _context.Error(DiagnosticCodes.UnresolvedSpecificationExampleType, $"Example '{example.Name}' must name an event, command, or read model, not a {type.Kind}; example inheritance is not supported.", example.Location);
                return;
            }

            _types[example] = type;
            ValidateDuplicates(example.Values.Concat(example.GeneratedValues), example.Location);
            foreach (var value in example.Values.Where(value => !type.Properties.Any(property => property.Name == value.Property)))
            {
                _context.Error(DiagnosticCodes.InvalidSpecificationExampleValue, $"Example '{example.Name}' property '{value.Property}' is not declared on current {type.Kind} '{example.Type}'; historical-only fields are not supported.", value.Location);
            }

            foreach (var value in example.Values.Where(value => type.Kind == "command" && type.Properties.Any(property => property.Name == value.Property && property.IsGenerated)))
            {
                _context.Error(DiagnosticCodes.InvalidSpecificationExampleValue, $"Example '{example.Name}' cannot supply generated property '{value.Property}' as input; use 'for' for its generated identifier or 'generated' for another value.", value.Location);
            }

            foreach (var value in example.GeneratedValues.Where(value => type.Kind != "command" || !type.Properties.Any(property => property.Name == value.Property && property.IsGenerated && !property.IsIdentifier)))
            {
                _context.Error(DiagnosticCodes.InvalidSpecificationExampleValue, $"Example '{example.Name}' generated fixture '{value.Property}' must name a nonidentifier generated command property.", value.Location);
            }

            if (type.Kind == "readmodel" && example.For is not null)
            {
                _context.Error(DiagnosticCodes.InvalidSpecificationExampleValue, $"Read-model example '{example.Name}' cannot contain 'for'; state the identifier as a property.", example.For.Location);
            }

            SpecificationValueConsistencyValidator.ValidateValues(example.Values.Concat(example.GeneratedValues), type.Properties, _declarations!, _context);
        }

        void ValidateDuplicates(IEnumerable<PropertyMappingSyntax> values, SourceLocation location)
        {
            foreach (var duplicate in values.GroupBy(value => value.Property, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                _context.Error(DiagnosticCodes.DuplicateSpecificationAssignment, $"Specification fixture assigns property '{duplicate.Key}' more than once.", location);
            }
        }

        FeatureSyntax ExpandFeature(FeatureSyntax feature) => feature with
        {
            Features = [.. feature.Features.Select(ExpandFeature)],
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Specifications = [.. slice.Specifications.Select(specification => ExpandSpecification(specification, _slices.First(entry => ReferenceEquals(entry.Slice, slice)).Scope))]
            })]
        };

        SpecificationSyntax ExpandSpecification(SpecificationSyntax specification, DeclarationScope scope)
        {
            var steps = new List<EffectiveSpecificationStep>();
            var effective = specification with
            {
                Given = [.. specification.Given.Select(step => ExpandEvent(step, "given", scope, steps))],
                GivenReadModels = specification.GivenReadModels is null ? null : [.. specification.GivenReadModels.Select(step => ExpandReadModel(step, "given readmodel", scope, steps))],
                When = specification.When is { } when ? ExpandCommand(when, scope, steps) : null,
                WhenAppended = specification.WhenAppended is { } append ? ExpandEvent(append, "when append", scope, steps) : null,
                ThenEvents = [.. specification.ThenEvents.Select(step => ExpandEvent(step, "then", scope, steps))],
                ThenReadModels = specification.ThenReadModels is null ? null : [.. specification.ThenReadModels.Select(step => ExpandReadModel(step, "then readmodel", scope, steps))]
            };
            _specifications.Add(new(specification, effective, [.. steps]));

            return effective;
        }

        (SpecificationExampleSyntax? Example, Entry? Type) ExampleFor(string name, string kind, string role, DeclarationScope scope, SourceLocation location)
        {
            var entry = Resolve(name, scope);
            if (entry is null && _entries.Exists(candidate => candidate.Kind == "example" && candidate.Declaration.Name == name.Split('.')[^1]))
            {
                _context.Error(DiagnosticCodes.UnresolvedSpecificationExampleType, $"Example reference '{name}' is unknown or ambiguous; qualify its declaration scope.", location);
            }

            if (entry?.Node is not SpecificationExampleSyntax example) return (null, null);
            if (!_types.TryGetValue(example, out var type)) return (example, null);
            if (type.Kind != kind)
            {
                var corrected = CorrectedRole(type.Kind, role);
                _context.Error(DiagnosticCodes.SpecificationExampleKindMismatch, $"Example '{name}' is a {type.Kind} example, not a {kind}; write '{corrected} {name}'.", location);
                return (example, null);
            }

            return (example, type);
        }

        SpecificationEventSyntax ExpandEvent(SpecificationEventSyntax step, string role, DeclarationScope scope, List<EffectiveSpecificationStep> steps)
        {
            var (example, type) = ExampleFor(step.EventType, "event", role, scope, step.Location);
            var inherited = type is null ? [] : example!.Values;
            var source = type is null ? null : example!.For;
            var effective = type is null ? step : step with { EventType = Qualified(type), Values = Merge(inherited, step.Values), For = step.For ?? source };
            steps.Add(new(role, step, effective, example, [.. Origins(inherited, step.Values), .. ForOrigin(source, step.For)]));

            return effective;
        }

        SpecificationCommandSyntax ExpandCommand(SpecificationCommandSyntax step, DeclarationScope scope, List<EffectiveSpecificationStep> steps)
        {
            var (example, type) = ExampleFor(step.CommandType, "command", "when", scope, step.Location);
            var inherited = type is null ? [] : example!.Values;
            var generated = type is null ? [] : example!.GeneratedValues;
            var source = type is null ? null : example!.For;
            var effective = type is null ? step : step with { CommandType = Qualified(type), Values = Merge(inherited, step.Values), GeneratedValues = Merge(generated, step.GeneratedValues), For = step.For ?? source };
            steps.Add(new("when", step, effective, example, [.. Origins(inherited, step.Values), .. Origins(generated, step.GeneratedValues, "generated "), .. ForOrigin(source, step.For)]));

            return effective;
        }

        SpecificationReadModelSyntax ExpandReadModel(SpecificationReadModelSyntax step, string role, DeclarationScope scope, List<EffectiveSpecificationStep> steps)
        {
            var (example, type) = ExampleFor(step.Name, "readmodel", role, scope, step.Location);
            var inherited = type is null ? [] : example!.Values;
            var effective = type is null ? step : step with { Name = Qualified(type), Properties = Merge(inherited, step.Properties) };
            steps.Add(new(role, step, effective, example, [.. Origins(inherited, step.Properties)]));

            return effective;
        }

        sealed record Entry(Declaration Declaration, SyntaxNode Node, string Kind, IEnumerable<PropertySyntax> Properties);
    }
}
