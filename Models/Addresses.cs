namespace Project_K.Models
{
    public class Addresses
    {
        public int Id { get; set; }
        public required string Address { get; set; }

        public ICollection<Members> Members { get; set; }
    }
}
