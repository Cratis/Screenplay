// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_exporting_source_declarations
{
    [Theory]
    [InlineData("Account\n")]
    [InlineData("Account ")]
    [InlineData("Account\u0000")]
    [InlineData("Account.Name")]
    void should_refuse_invalid_parent_and_child_names_before_export(string name)
    {
        Refused(new(name, SourceLocation.Start));
        Refused(new("Account", SourceLocation.Start) { Streams = [new(name, SourceLocation.Start)] });
    }

    [Theory]
    [InlineData("String\n")]
    [InlineData("String ")]
    [InlineData("String\u0000")]
    [InlineData("String optional")]
    void should_refuse_non_roundtripping_identifier_type_names(string name)
    {
        var type = new TypeRefSyntax(name, false, false, SourceLocation.Start);
        Refused(new("Account", SourceLocation.Start) { Identifier = type });
        Refused(new("Account", SourceLocation.Start) { Streams = [new("Transactions", SourceLocation.Start) { StreamId = type }] });
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    void should_refuse_optional_or_collection_identifier_shapes(bool optional, bool collection)
    {
        var type = new TypeRefSyntax("Unknown", collection, optional, SourceLocation.Start);
        Refused(new("Account", SourceLocation.Start) { Identifier = type });
        Refused(new("Account", SourceLocation.Start) { Streams = [new("Transactions", SourceLocation.Start) { StreamId = type }] });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    void should_refuse_blank_rename_pins(string pin)
    {
        Refused(new("Account", SourceLocation.Start) { Id = pin });
        Refused(new("Account", SourceLocation.Start) { Streams = [new("Transactions", SourceLocation.Start) { Id = pin }] });
    }

    [Fact]
    void should_refuse_null_stream_collections_and_elements()
    {
        Refused(new("Account", SourceLocation.Start) { Streams = null! });
        Refused(new("Account", SourceLocation.Start) { Streams = [null!] });
    }

    [Fact]
    void should_preserve_valid_unicode_names_and_unbound_identifier_types_without_semantic_validation()
    {
        var application = Application(new("Accountß", SourceLocation.Start)
        {
            Id = "Old\nAccount", Identifier = new("Contracts.Unknown", false, false, SourceLocation.Start),
            Streams = [new("Transactionsα", SourceLocation.Start) { Description = "History", Id = "OldTransactions" }]
        });
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(application));
        var printed = new ScreenplayPrinter().Print(restored);
        SyntaxJson.StructurallyEqual(restored, new ScreenplayCompiler().Parse(printed).Value!).ShouldBeTrue();
        new PlayFileWriter().Expand(restored).Single().Content.ShouldEqual(printed);
    }

    static ApplicationSyntax Application(EventSourceSyntax source) => new([], [], [], [], SourceLocation.Start) { EventSources = [source] };

    static void Refused(EventSourceSyntax source)
    {
        var application = Application(source);
        Catch.Exception(() => SyntaxJson.Serialize(application)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => new ScreenplayPrinter().Print(application)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => _ = new PlayFileWriter().Expand(application).ToArray()).ShouldBeOfExactType<InvalidSyntaxJson>();
    }
}
