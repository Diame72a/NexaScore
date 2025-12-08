using System;
using System.ComponentModel.DataAnnotations;

namespace Projet.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Titre { get; set; } = string.Empty; 
        public string Message { get; set; } = string.Empty; 
        public string? LinkAction { get; set; } 
        public string IconClass { get; set; } = "fas fa-info-circle"; 
        public string ColorClass { get; set; } = "text-primary"; 

        public bool IsRead { get; set; } = false;
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}