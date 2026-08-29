using System;
using System.Collections.Generic;

namespace ProjectK.Common.Models.Dtos.UsersModule
{
    public record TileLayoutDto(
        string BoardKey,
        IReadOnlyList<string> TileKeys,
        int SchemaVersion,
        DateTime UpdatedAtUtc);

    public record SaveTileLayoutRequestDto(IReadOnlyList<string> TileKeys, int SchemaVersion = 1);
}
