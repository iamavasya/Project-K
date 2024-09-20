using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_K.Infrastructure.Models
{
    public class MemberLevel
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Member))]
        public int MemberId { get; set; }
        public Member Member { get; set; }

        [ForeignKey(nameof(Level))]
        public int LevelId { get; set; }
        public Level Level { get; set; }
        public DateOnly? AchieveDate { get; set; }
    }
}
