// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// In a block that also reads property lines, <c>file</c> is told from a property named <c>file</c> by shape -
/// a type reference is a bare identifier, so anything carrying a separator or an extension is a path and
/// nothing else. The property wins the tie, so a document written before the directive existed keeps meaning
/// what it meant.
/// </summary>
public class when_compiling_a_declaration_with_a_property_named_file : given.a_compiler
{
    const string Source =
        """
        concept Attachment : String

        type Upload
          file Attachment
          size Int

        module Invoicing
          feature Invoices
            slice StateChange AttachDocument
              event DocumentAttached
                file Attachment

              readmodel Document
                file Attachment
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_type_line_as_a_property() => Type.Properties.First().Name.ShouldEqual("file");
    [Fact] void should_leave_the_type_without_a_file_reference() => Type.File.ShouldBeNull();
    [Fact] void should_read_the_event_line_as_a_property() => Event.Properties.Single().Name.ShouldEqual("file");
    [Fact] void should_leave_the_event_without_a_file_reference() => Event.File.ShouldBeNull();
    [Fact] void should_read_the_readmodel_line_as_a_property() => ReadModel.Properties.Single().Name.ShouldEqual("file");
    [Fact] void should_leave_the_readmodel_without_a_file_reference() => ReadModel.File.ShouldBeNull();

    TypeSyntax Type => _result.Value!.Types!.Single();
    SliceSyntax Slice => _result.Value!.Modules.Single().Features.Single().Slices.Single();
    EventSyntax Event => Slice.Events.Single();
    ReadModelSyntax ReadModel => Slice.ReadModels!.Single();
}
