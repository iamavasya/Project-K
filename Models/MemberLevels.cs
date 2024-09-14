namespace Project_K.Models
{
    public class MemberLevels
    {
        public int Id { get; set; }
        public Members Member { get; set; }
        public Levels Level { get; set; }
        public required DateOnly AchieveDate { get; set; }
    }
}
