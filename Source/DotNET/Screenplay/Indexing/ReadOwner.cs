// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Indexing;

sealed record ReadOwner(string Kind, string Name, string Address, SourceLocation Location);
