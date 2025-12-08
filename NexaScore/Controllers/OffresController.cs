using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services; // IMPORTANT
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Projet.Controllers
{
    [Authorize]
    public class OffresController : Controller
    {
        private readonly ProjetContext _context;
        private readonly INotificationService _notifService;

        public OffresController(ProjetContext context, INotificationService notifService)
        {
            _context = context;
            _notifService = notifService;
        }

        // ============================================================
        // 1. INDEX (LISTE AVEC FILTRES & DASHBOARD)
        // ============================================================
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

            // Stats pour le graphique (Répartition Géo)
            var statsVilles = offres
                .GroupBy(o => o.VilleCible)
                .Select(g => new { Ville = g.Key, Nombre = g.Count() })
                .ToList();

            ViewBag.VillesLabels = statsVilles.Select(s => s.Ville).ToList();
            ViewBag.VillesData = statsVilles.Select(s => s.Nombre).ToList();

            // Data pour la Vue
            ViewBag.Postes = await _context.Postes.OrderBy(p => p.Intitule).ToListAsync();
            ViewBag.TotalCandidats = await _context.Personnes.CountAsync(); // KPI Vivier

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentPoste"] = posteId;

            return View(offres);
        }

        // ============================================================
        // 2. CREATE (WIZARD ÉTAPE 1)
        // ============================================================
        public IActionResult Create()
        {
            ViewBag.PosteId = new SelectList(_context.Postes, "Id", "Intitule");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Offre offre)
        {
            offre.DateCreation = DateTime.Now;
            ModelState.Remove("Poste");
            ModelState.Remove("CompetenceSouhaitees"); // Étape 2

            if (ModelState.IsValid)
            {
                // 1. Création de l'offre
                _context.Add(offre);
                await _context.SaveChangesAsync();

                // 2. Création des paramètres de scoring par défaut
                var paramsScoring = new ParametreScoring { OffreId = offre.Id };
                _context.Add(paramsScoring);
                await _context.SaveChangesAsync();

                // 3. Notification Automatique
                await _notifService.Ajouter(
                    "Nouvelle Offre",
                    $"Le poste '{offre.Titre}' a été ouvert.",
                    "fas fa-briefcase",
                    "text-primary",
                    Url.Action("Details", "Offres", new { id = offre.Id })
                );

                TempData["SuccessMessage"] = "Offre créée ! Définissez maintenant les compétences.";
                return RedirectToAction(nameof(Edit), new { id = offre.Id });
            }

            ViewBag.PosteId = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);
            return View(offre);
        }

        // ============================================================
        // 3. EDIT (WIZARD ÉTAPE 2 : COMPÉTENCES & SCORING)
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var offre = await _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null) return NotFound();

            if (offre.ParametreScoring == null) offre.ParametreScoring = new ParametreScoring();

            ViewBag.PosteId = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);
            return View(offre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Offre offre)
        {
            if (id != offre.Id) return NotFound();

            ModelState.Remove("Poste");
            ModelState.Remove("CompetenceSouhaitees");
            ModelState.Remove("ParametreScoring.Offre");

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.Offres
                        .Include(o => o.ParametreScoring)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (original == null) return NotFound();

                    // Update champs classiques
                    original.Titre = offre.Titre;
                    original.Description = offre.Description;
                    original.VilleCible = offre.VilleCible;
                    original.CodePostalCible = offre.CodePostalCible;
                    original.PosteId = offre.PosteId;

                    // Update Paramètres Scoring
                    if (original.ParametreScoring == null)
                    {
                        original.ParametreScoring = new ParametreScoring { OffreId = original.Id };
                    }

                    if (offre.ParametreScoring != null)
                    {
                        original.ParametreScoring.CibleExperience = offre.ParametreScoring.CibleExperience;
                    }

                    _context.Update(original);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Configuration sauvegardée.";
                }
                catch (DbUpdateConcurrencyException) { throw; }

                return RedirectToAction(nameof(Edit), new { id = id }); // Reste sur la page pour continuer
            }

            ViewBag.PosteId = new SelectList(_context.Postes, "Id", "Intitule", offre.PosteId);
            return View(offre);
        }

        // ============================================================
        // 4. GESTION DES COMPÉTENCES (MODALE AJAX)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> AjouterPlusieurs(int id)
        {
            var offre = await _context.Offres.Include(o => o.CompetenceSouhaitees).FirstOrDefaultAsync(o => o.Id == id);
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
                    NiveauRequis = 3 // Niveau par défaut (intermédiaire)
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
                    _context.Add(new CompetenceSouhaitee
                    {
                        OffreId = model.OffreId,
                        CompetenceId = item.CompetenceId,
                        NiveauRequis = item.NiveauRequis
                    });
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = model.OffreId });
        }

        [HttpPost]
        public async Task<IActionResult> SupprimerCompetence(int id, string source)
        {
            var liaison = await _context.CompetenceSouhaitees.FindAsync(id);
            if (liaison != null)
            {
                int offreId = liaison.OffreId;
                _context.CompetenceSouhaitees.Remove(liaison);
                await _context.SaveChangesAsync();

                if (source == "Edit") return RedirectToAction(nameof(Edit), new { id = offreId });
                return RedirectToAction(nameof(Details), new { id = offreId });
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 5. DETAILS & DELETE
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var offre = await _context.Offres
                .Include(o => o.Poste)
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offre == null) return NotFound();
            return View(offre);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var offre = await _context.Offres.Include(o => o.Poste).FirstOrDefaultAsync(m => m.Id == id);
            return offre == null ? NotFound() : View(offre);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offre = await _context.Offres.FindAsync(id);
            if (offre != null)
            {
                string titre = offre.Titre;
                _context.Offres.Remove(offre);
                await _context.SaveChangesAsync();

                // Notification suppression
                await _notifService.Ajouter(
                    "Offre Supprimée",
                    $"Le poste '{titre}' a été retiré.",
                    "fas fa-trash",
                    "text-danger"
                );
            }
            return RedirectToAction(nameof(Index));
        }
    }
}