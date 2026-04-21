namespace ProjectK.BusinessLogic.Modules.AuthModule.Models
{
    public record ZbtStatsDto
    {
        public int CurrentActiveUsers { get; init; }
        public int BetaCap { get; init; }
        public bool IsCapReached => CurrentActiveUsers >= BetaCap;
        public string? KurinName { get; init; }
        public string? Scope { get; init; } // "Global" or "Kurin"
    }
}
