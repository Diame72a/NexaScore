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
    public class OffresController : Controller
    {
        private readonly ProjetContext _context;

        public OffresController(ProjetContext context)
        {
            _context = context;
        }

        // GET: Offres
        public async Task<IActionResult> Index(string searchString, int? posteId)
        {

            var query = _context.Offres.Include(o => o.Poste).AsQueryable();


            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(o => o.Titre.Contains(searchString) || o.VilleCible.Contains(searchString));
            }


            if (posteId.HasValue)
            {
                query = query.Where(o => o.PosteId == posteId);
            }


            var offres = await query.OrderByDescending(o => o.DateCreation).ToListAsync();




            ViewBag.Postes = await _context.Postes.OrderBy(p => p.Intitule).ToListAsync();


            var statsVilles = offres
                .GroupBy(o => o.VilleCible)
                .Select(g => new { Ville = g.Key, Nombre = g.Count() })
                .ToList();

            ViewBag.VillesLabels = statsVilles.Select(s => s.Ville).ToList();
            ViewBag.VillesData = statsVilles.Select(s => s.Nombre).ToList();


            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentPoste"] = posteId;

            return View(offres);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var offre = await _context.Offres
                .Include(o => o.Poste)
                .Include(o => o.CompetenceSouhaitees)
                    .ThenInclude(cs => cs.Competence)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offre == null) return NotFound();


            ViewBag.CompetencesDispos = _context.Competences.OrderBy(c => c.Nom).ToList();

            return View(offre);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterCompetence(int offreId, int competenceId, int niveau)
        {

            var existe = await _context.CompetenceSouhaitees
                .AnyAsync(cs => cs.OffreId == offreId && cs.CompetenceId == competenceId);

            if (!existe)
            {
                var nouvelleExigence = new CompetenceSouhaitee
                {
                    OffreId = offreId,
                    CompetenceId = competenceId,
                    NiveauRequis = niveau
                };
                _context.Add(nouvelleExigence);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = offreId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerCompetence(int id)
        {
            var liaison = await _context.CompetenceSouhaitees.FindAsync(id);
            if (liaison != null)
            {
                int offreId = liaison.OffreId;
                _context.CompetenceSouhaitees.Remove(liaison);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Edit), new { id = offreId });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Offres/Create
        public IActionResult Create()
        {

            ViewData["PosteId"] = new SelectList(_context.Postes, "Id", "Intitule");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,VilleCible,CodePostalCible,PosteId")] Offre offre)
        {
            offre.DateCreation = DateTime.Now;
            ModelState.Remove("Poste");

            if (ModelState.IsValid)
            {
                _context.Add(offre);
                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] = "Offre créée ! Définissez maintenant les compétences requises.";
                return RedirectToAction(nameof(Edit), new { id = offre.Id });
            }

            ViewData["PosteId"] = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);
            return View(offre);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();


            var offre = await _context.Offres
                .Include(o => o.CompetenceSouhaitees)
                    .ThenInclude(cs => cs.Competence)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null) return NotFound();


            ViewData["PosteId"] = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);


            ViewBag.CompetencesDispos = _context.Competences.OrderBy(c => c.Nom).ToList();

            return View(offre);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,DateCreation,VilleCible,CodePostalCible,PosteId")] Offre offre)
        {
            if (id != offre.Id)
            {
                return NotFound();
            }


            ModelState.Remove("Poste");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(offre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OffreExists(offre.Id))
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


            ViewData["PosteId"] = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);


            var offreComplete = await _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .FirstOrDefaultAsync(o => o.Id == id);


            if (offreComplete != null) offre.CompetenceSouhaitees = offreComplete.CompetenceSouhaitees;
            ViewBag.CompetencesDispos = _context.Competences.OrderBy(c => c.Nom).ToList();

            return View(offre);
        }

        // GET: Offres/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offre = await _context.Offres
                .Include(o => o.Poste)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offre == null)
            {
                return NotFound();
            }

            return View(offre);
        }

        // POST: Offres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offre = await _context.Offres.FindAsync(id);
            if (offre != null)
            {
                _context.Offres.Remove(offre);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> AjouterPlusieurs(int id)
        {
            var offre = await _context.Offres
                .Include(o => o.CompetenceSouhaitees)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null) return NotFound();


            var idsExistants = offre.CompetenceSouhaitees.Select(c => c.CompetenceId).ToList();


            var competencesDispo = await _context.Competences
                .Where(c => !idsExistants.Contains(c.Id))
                .OrderBy(c => c.Nom)
                .ToListAsync();

            var model = new OffreBulkCompetenceViewModel
            {
                OffreId = id,
                Competences = competencesDispo.Select(c => new CompetenceSelectionItem
                {
                    CompetenceId = c.Id,
                    Nom = c.Nom,
                    EstSelectionne = false,
                    NiveauRequis = 3
                }).ToList()
            };


            return PartialView("_AjoutMultiplePartial", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterPlusieurs(OffreBulkCompetenceViewModel model)
        {

            var aAjouter = model.Competences.Where(c => c.EstSelectionne).ToList();

            if (aAjouter.Any())
            {
                foreach (var item in aAjouter)
                {
                    var nouvelleLiaison = new CompetenceSouhaitee
                    {
                        OffreId = model.OffreId,
                        CompetenceId = item.CompetenceId,
                        NiveauRequis = item.NiveauRequis
                    };
                    _context.Add(nouvelleLiaison);
                }
                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Edit), new { id = model.OffreId });
        }

        private bool OffreExists(int id)
        {
            return _context.Offres.Any(e => e.Id == id);
        }
    }
}