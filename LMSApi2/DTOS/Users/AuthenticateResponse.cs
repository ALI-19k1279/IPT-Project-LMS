﻿using LMSApi2.Models;


namespace LMSApi2.DTOS.Users
{
    public class AuthenticateResponse
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public Role Role { get; set; }
        public string? Token { get; set; }

        public AuthenticateResponse(User user, string token)
        {
            Id = user.UserId;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Role = user.Role;
            Token = token;
        }
    }
}
