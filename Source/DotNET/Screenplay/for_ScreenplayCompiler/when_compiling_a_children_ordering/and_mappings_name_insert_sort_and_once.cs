// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

// Ordering lines and 'set once' are keyword lines; a mapping that targets a property named insert, sort or once stays a plain mapping.
public class and_mappings_name_insert_sort_and_once : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection("""
        projection Models => ModelReadModel
          children entries identified by entryId
            from EntryAdded
              insert = title
              sort = rank
              once = title
              set @once = title
              set insert to title
        """);

    static readonly string[] _properties = ["insert", "sort", "once", "once", "insert"];

    MappingSyntax[] Mappings => [.. Children.Blocks.OfType<FromSyntax>().Single().Mappings];

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_declare_no_ordering() => Children.Ordering.ShouldBeNull();
    [Fact] void should_keep_every_line_a_plain_mapping() => Mappings.All(mapping => mapping is SetMappingSyntax).ShouldBeTrue();
    [Fact] void should_target_the_named_properties() => Mappings.Select(mapping => mapping.Property).ShouldEqual(_properties);
}
