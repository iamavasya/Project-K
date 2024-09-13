namespace Project_K.Models
{
    public class MemberLevels
    {
        public int Id { get; set; }
        public required int MemberId { get; set; }
        public required int LevelId { get; set; }
        public required DateOnly AchieveDate { get; set; }
    }
}
