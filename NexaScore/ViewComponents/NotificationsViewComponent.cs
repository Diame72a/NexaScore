using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Projet.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly ProjetContext _context;

        public NotificationsViewComponent(ProjetContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await _context.Notifications
                .OrderByDescending(n => n.DateCreation)
                .Take(5)
                .ToListAsync();

            return View(items);
        }
    }
}