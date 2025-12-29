using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PokedexApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;

        public string EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpires { get; set; }

        public string PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpires { get; set; }

        public string RefreshToken { get; set; }

        public DateTime? RefreshTokenExpires { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}