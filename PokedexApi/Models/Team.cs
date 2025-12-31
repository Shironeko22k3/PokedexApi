using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokedexApi.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        public string TeamData { get; set; } = string.Empty; 

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}