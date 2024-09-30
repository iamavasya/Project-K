using System.ComponentModel.DataAnnotations;

namespace Project_K.Infrastructure.Models
{
    public class Team
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        public ICollection<Member> Members { get; set; }
    }
}
