﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Project_K.Models
{
    public class Member
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string MiddleName { get; set; }
        public string? Nickname { get; set; }
        public required DateOnly BirthDate { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string Telegram { get; set; }
        public required DateOnly PlastJoin { get; set; }
        public int AddressId { get; set; }
        public Address Address { get; set; }
        public int SchoolId { get; set; }
        public School School { get; set; }
        public int KurinLevelId { get; set; }
        public KurinLevel KurinLevel { get; set; }
        public ICollection<MemberLevel> MemberLevels { get; set; }
    }

}