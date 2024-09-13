using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Project_K.Models
{
    [Table("Members")]
    public class Members
    {
        [Key]
        public uint Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(255)]
        public required string LastName { get; set; }

        [MaxLength(255)]
        public required string MiddleName { get; set; }

        [MaxLength(255)]
        public string? Nickname { get; set; }

        [Required]
        public required DateTime BirthDate { get; set; }

        [Required]
        [ForeignKey("Address")]
        public ulong AddressID { get; set; }

        [Required]
        [ForeignKey("School")]
        public ulong SchoolID { get; set; }

        [Required]
        [MaxLength(20)]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Telegram { get; set; }

        [Required]
        public required DateTime PlastJoin { get; set; }

        public Addresses Address { get; set; }
        public Schools School { get; set; }
    }

}