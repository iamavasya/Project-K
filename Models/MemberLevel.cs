namespace Project_K.Models
{
    public class MemberLevel
    {
        public int Id { get; set; }
        public Member Member { get; set; }
        public Level Level { get; set; }
        public required DateOnly AchieveDate { get; set; }
    }
}
