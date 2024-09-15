namespace Project_K.Models
{
    public class KurinLevel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required int RequiredPoints { get; set; }

        public ICollection<Member> Members { get; set; }
    }
}
