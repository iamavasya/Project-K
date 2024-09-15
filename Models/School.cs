namespace Project_K.Models
{
    public class School
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        public ICollection<Member> Members { get; set; }
    }
}
