// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_consistency_depends_on_unknown_shapes : given.a_compiler
{
    const string Source =
        """
        import External.Payload
        import External.Kind
        import External.Identity
        import External.ImportedEvent
        type Row
          id Uuid
          note String
        module Local
          feature Entries
            slice StateChange Record
              command RecordEntry
                payload Payload
                kind Kind
                id Uuid
                validate
                  payload.unknown not empty
                reads View by id
                produces ImportedEvent
                  absent = payload.unknown
              specification UnknownShapes
                when RecordEntry
                  kind = Anything
                then ImportedEvent
                  absent = "not statically known"
            slice StateView Browse
              readmodel View
                rows Row[]
              projection Entries => View
                children rows identified by id
                  from ImportedEvent
              query Lookup => View
                by identity Identity
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_leave_unknown_property_paths_read_types_enum_values_and_automap_sources_undecided() => _result.Diagnostics.ShouldBeEmpty();
}
