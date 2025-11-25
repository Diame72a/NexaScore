using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projet.Models;

namespace Projet.Controllers
{
    public class CompetencesController : Controller
    {
        private readonly ProjetContext _context;

        public CompetencesController(ProjetContext context)
        {
            _context = context;
        }

        // GET: Competences
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            
            var query = _context.Competences.AsQueryable();

            
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Nom.Contains(searchString));
            }

            
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            switch (sortOrder)
            {
                case "name_desc":
                    query = query.OrderByDescending(c => c.Nom);
                    break;
                default:
                    query = query.OrderBy(c => c.Nom);
                    break;
            }

            var competences = await query.ToListAsync();

            
            var topCompetences = await _context.CompetenceSouhaitees
                .Include(cs => cs.Competence)
                .GroupBy(cs => cs.Competence.Nom)
                .Select(g => new { Nom = g.Key, Nombre = g.Count() })
                .OrderByDescending(x => x.Nombre).Take(5).ToListAsync();

            ViewBag.TopLabels = topCompetences.Select(x => x.Nom).ToList();
            ViewBag.TopData = topCompetences.Select(x => x.Nombre).ToList();

            
            ViewData["CurrentFilter"] = searchString;

            return View(competences);
        }

        // GET: Competences/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competence = await _context.Competences
                .FirstOrDefaultAsync(m => m.Id == id);
            if (competence == null)
            {
                return NotFound();
            }

            return View(competence);
        }

        // GET: Competences/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Competences/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom")] Competence competence)
        {
            if (ModelState.IsValid)
            {
                _context.Add(competence);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(competence);
        }

        // GET: Competences/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competence = await _context.Competences.FindAsync(id);
            if (competence == null)
            {
                return NotFound();
            }
            return View(competence);
        }

        // POST: Competences/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom")] Competence competence)
        {
            if (id != competence.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(competence);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompetenceExists(competence.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(competence);
        }

        // GET: Competences/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competence = await _context.Competences
                .FirstOrDefaultAsync(m => m.Id == id);
            if (competence == null)
            {
                return NotFound();
            }

            return View(competence);
        }

        // POST: Competences/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competence = await _context.Competences.FindAsync(id);
            if (competence != null)
            {
                _context.Competences.Remove(competence);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CompetenceExists(int id)
        {
            return _context.Competences.Any(e => e.Id == id);
        }
    }
}
