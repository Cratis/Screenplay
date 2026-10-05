// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_scaling_inline_event_names : given.a_compiler
{
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    void should_check_each_typed_property_name_once(int count)
    {
        var source = "produces event Recorded\n" + string.Join('\n', Enumerable.Range(0, count).Select(index => $"  value{index} String = \"value\""));
        var context = new ParserContext(new(SourceLineSplitter.Split(source)));
        var header = context.Reader.TakeSignificant();
        var comparer = new counting_names();
        var production = ProducesParser.Parse(context, header, true, comparer)!;
        comparer.HashCalls.ShouldEqual(count);
        context.Diagnostics.ShouldBeEmpty();
        production.InlineEvent!.Properties.Count().ShouldEqual(count);
        production.Mappings.Count().ShouldEqual(count);
    }

    [Fact]
    void should_keep_duplicate_diagnostics_and_mapping_order_with_ordinal_names()
    {
        const string Source = "produces event Recorded\n  valueA String = \"first\"\n  valuea String = \"second\"\n  @valueA String = \"third\"\n  valueA String = \"fourth\"";
        var context = new ParserContext(new(SourceLineSplitter.Split(Source)));
        var production = ProducesParser.Parse(context, context.Reader.TakeSignificant(), true)!;
        context.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual("PLAY0168@4", "PLAY0168@5");
        context.Diagnostics.All(diagnostic => diagnostic.Message == "Event 'Recorded' already declares property 'valueA'").ShouldBeTrue();
        production.InlineEvent!.Properties.Select(property => property.Name).ShouldEqual("valueA", "valuea", "valueA", "valueA");
        production.Mappings.Select(mapping => mapping.Property).ShouldEqual("valueA", "valuea", "valueA", "valueA");
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    void should_read_imported_short_names_once_per_validation(int count)
    {
        var source = "module Projects\n  feature Recording\n    slice StateChange Record\n      command Record\n        recordId Uuid identifier\n" +
            string.Join('\n', Enumerable.Range(0, count).Select(index => $"        produces event Recorded{index}"));
        var application = _compiler.Parse(source).Value!;
        var visits = 0;
        IEnumerable<ImportSyntax> Imports()
        {
            foreach (var index in Enumerable.Range(0, count))
            {
                visits++;
                yield return new($"Other.Imported{index}", SourceLocation.Start);
            }
        }

        var context = ParserContext.ForDiagnostics();
        InlineEventValidator.Validate(application with { Imports = Imports() }, [.. application.Modules.Single().Features.Single().Slices], context);
        visits.ShouldEqual(count);
        context.Diagnostics.ShouldBeEmpty();
    }

    sealed class counting_names : IEqualityComparer<string>
    {
        public int HashCalls { get; private set; }

        public bool Equals(string? x, string? y) => StringComparer.Ordinal.Equals(x, y);

        public int GetHashCode(string obj)
        {
            HashCalls++;
            return StringComparer.Ordinal.GetHashCode(obj);
        }
    }
}
