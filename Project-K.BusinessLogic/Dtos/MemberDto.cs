namespace Project_K.BusinessLogic.Dtos
{
    public class MemberDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string MiddleName { get; set; }
        public string? Nickname { get; set; }
        public required DateOnly BirthDate { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string Telegram { get; set; }
        public required DateOnly PlastJoin { get; set; }
        public string Address { get; set; }
        public string School { get; set; }
        public int SelectedLevelId { get; set; }
        public int KurinLevelId { get; set; }
        public int TeamId { get; set; }
        public string? UserId { get; set; }
    }
}
