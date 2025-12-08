using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Projet.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ProjetContext _context;

        public NotificationsController(ProjetContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var notifs = await _context.Notifications
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();

            return View(notifs);
        }

        
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();

            if (unread.Any())
            {
                foreach (var n in unread) n.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            var all = await _context.Notifications.ToListAsync();
            if (all.Any())
            {
                _context.Notifications.RemoveRange(all);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null)
            {
                _context.Notifications.Remove(notif);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}