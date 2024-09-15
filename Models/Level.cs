namespace Project_K.Models
{
    public class Level
    {
        public int Id { get; set; }
        public required int Name { get; set; }

        public ICollection<MemberLevel> MemberLevels { get; set; }
    }
}
