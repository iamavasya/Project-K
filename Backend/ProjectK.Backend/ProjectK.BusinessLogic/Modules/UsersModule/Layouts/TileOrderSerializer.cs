using System.Text.Json;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Layouts
{
    public static class TileOrderSerializer
    {
        public static IReadOnlyList<string> Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        public static string Serialize(IReadOnlyList<string> tileKeys)
        {
            return JsonSerializer.Serialize(tileKeys);
        }
    }
}
