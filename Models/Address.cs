namespace Project_K.Models
{
    public class Address
    {
        public int Id { get; set; }
        public required string AddressName { get; set; }

        public ICollection<Member> Members { get; set; }
    }
}
