namespace ProjectK.Common.Models.Dtos
{
    public sealed class NotificationQuery
    {
        public bool UnreadOnly { get; set; }
        public int Take { get; set; } = 50;
    }
}
