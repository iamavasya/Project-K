using System.ComponentModel.DataAnnotations;

namespace Project_K.Models
{
    public class Level
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        public ICollection<MemberLevel> MemberLevels { get; set; }

    }
}
