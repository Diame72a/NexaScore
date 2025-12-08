using Projet.Models;
using System;
using System.Threading.Tasks;

namespace Projet.Services
{
    // Interface pour pouvoir l'injecter partout
    public interface INotificationService
    {
        Task Ajouter(string titre, string message, string icon, string color, string? url = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ProjetContext _context;

        public NotificationService(ProjetContext context)
        {
            _context = context;
        }

        public async Task Ajouter(string titre, string message, string icon, string color, string? url = null)
        {
            var notif = new Notification
            {
                Titre = titre,
                Message = message,
                IconClass = icon,
                ColorClass = color,
                LinkAction = url,
                DateCreation = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
        }
    }
}