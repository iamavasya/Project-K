using System.ComponentModel.DataAnnotations;

namespace Project_K.Models
{
    public class KurinLevel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int RequiredPoints { get; set; }

        public ICollection<Member> Members { get; set; }
    }
}
