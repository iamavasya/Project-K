using System.ComponentModel.DataAnnotations;

namespace Project_K.Models
{
    public class Address
    {
        public int Id { get; set; }
        
        [Required]
        public string AddressName { get; set; }

        public ICollection<Member> Members { get; set; }
    }
}
