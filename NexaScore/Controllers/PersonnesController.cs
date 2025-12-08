using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Projet.Controllers
{
    public class PersonnesController : Controller
    {
        private readonly ProjetContext _context;

        public PersonnesController(ProjetContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. INDEX (LISTE AVEC RECHERCHE ET DASHBOARD)
        // ============================================================
        public async Task<IActionResult> Index(string searchString, string villeFilter)
        {
            var query = _context.Personnes.AsQueryable();

            // 1. Filtre Global (Nom, Prénom, Email + NOUVEAU: TitreJobActuel)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Nom.Contains(searchString)
                                      || p.Prenom.Contains(searchString)
                                      || p.Email.Contains(searchString)
                                      || (p.TitreJobActuel != null && p.TitreJobActuel.Contains(searchString)));
            }

            // 2. Filtre Ville
            if (!string.IsNullOrEmpty(villeFilter))
            {
                query = query.Where(p => p.Ville == villeFilter);
            }

            // 3. Récupération des résultats
            var candidats = await query.OrderByDescending(p => p.Id).ToListAsync();

            // --- DONNÉES POUR LA VUE (Statistiques & Filtres) ---

            // Dropdown Villes
            ViewBag.Villes = await _context.Personnes
                .Where(p => p.Ville != null)
                .Select(p => p.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            // Stats Rapides pour le Dashboard
            ViewBag.TotalCandidats = await _context.Personnes.CountAsync();
            ViewBag.TotalExperts = await _context.Personnes.CountAsync(p => (p.AnneesExperienceTotal ?? 0) >= 5);

            // Filtres actuels (pour garder la sélection dans la vue)
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentVille"] = villeFilter;

            return View(candidats);
        }

        // ============================================================
        // 2. CREATE (CRÉATION ÉTAPE 1)
        // ============================================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Personne personne)
        {
            // On ignore la validation des compétences (étape suivante)
            ModelState.Remove("CompetenceAcquises");

            // VALIDATION DATE (DateOnly)
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (personne.DateNaissance > today)
                ModelState.AddModelError("DateNaissance", "La date de naissance ne peut pas être dans le futur.");

            if (personne.DateNaissance.Year < 1900)
                ModelState.AddModelError("DateNaissance", "Année de naissance invalide.");

            if (ModelState.IsValid)
            {
                _context.Add(personne);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profil créé ! Complétez maintenant les informations.";
                return RedirectToAction(nameof(Edit), new { id = personne.Id });
            }
            return View(personne);
        }

        // ============================================================
        // 3. EDIT (MODIFICATION COMPLÈTE)
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(c => c.Competence)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personne == null) return NotFound();

            return View(personne);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Personne personne)
        {
            if (id != personne.Id) return NotFound();

            ModelState.Remove("CompetenceAcquises");

            // Validation DateOnly
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (personne.DateNaissance > today)
                ModelState.AddModelError("DateNaissance", "Date invalide.");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Récupération de l'objet original
                    var original = await _context.Personnes.FindAsync(id);
                    if (original == null) return NotFound();

                    // 2. Mise à jour des champs (Y compris les nouveaux)
                    original.Nom = personne.Nom;
                    original.Prenom = personne.Prenom;
                    original.Email = personne.Email;
                    original.Telephone = personne.Telephone;         // Nouveau
                    original.TitreJobActuel = personne.TitreJobActuel; // Nouveau
                    original.Description = personne.Description;     // Nouveau
                    original.Ville = personne.Ville;
                    original.CodePostal = personne.CodePostal;
                    original.DateNaissance = personne.DateNaissance;
                    original.AnneesExperienceTotal = personne.AnneesExperienceTotal;

                    // 3. Sauvegarde
                    _context.Update(original);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Profil mis à jour avec succès.";

                    // On recharge la page Edit pour voir les changements
                    return RedirectToAction(nameof(Edit), new { id = personne.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Personnes.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("Email", "Erreur lors de la sauvegarde (Email en double ?).");
                }
            }

            // En cas d'erreur de validation, on recharge les compétences pour l'affichage
            var pComplet = await _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(c => c.Competence)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pComplet != null)
                personne.CompetenceAcquises = pComplet.CompetenceAcquises;

            return View(personne);
        }

        // ============================================================
        // 4. GESTION DES COMPÉTENCES (BULK ADD & DELETE)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AjouterPlusieurs(int id)
        {
            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personne == null) return NotFound();

            var idsExistants = personne.CompetenceAcquises.Select(c => c.CompetenceId).ToList();
            var competencesDispo = await _context.Competences
                .Where(c => !idsExistants.Contains(c.Id))
                .OrderBy(c => c.Nom)
                .ToListAsync();

            var model = new PersonneBulkCompetenceViewModel
            {
                PersonneId = id,
                Competences = competencesDispo.Select(c => new CompetenceSelectionItem
                {
                    CompetenceId = c.Id,
                    Nom = c.Nom,
                    EstSelectionne = false,
                    NiveauRequis = 2 // Niveau par défaut
                }).ToList()
            };

            return PartialView("_AjoutMultiplePartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterPlusieurs(PersonneBulkCompetenceViewModel model)
        {
            var aAjouter = model.Competences.Where(c => c.EstSelectionne).ToList();

            if (aAjouter.Any())
            {
                foreach (var item in aAjouter)
                {
                    _context.Add(new CompetenceAcquise
                    {
                        PersonneId = model.PersonneId,
                        CompetenceId = item.CompetenceId,
                        Niveau = item.NiveauRequis
                    });
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = model.PersonneId });
        }

        [HttpPost]
        public async Task<IActionResult> SupprimerCompetence(int id, string source)
        {
            var comp = await _context.CompetenceAcquises.FindAsync(id);
            if (comp != null)
            {
                int pid = comp.PersonneId;
                _context.CompetenceAcquises.Remove(comp);
                await _context.SaveChangesAsync();

                if (source == "Edit") return RedirectToAction(nameof(Edit), new { id = pid });
                return RedirectToAction(nameof(Details), new { id = pid });
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 5. DETAILS & DELETE
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(c => c.Competence)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personne == null) return NotFound();

            return View(personne);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var personne = await _context.Personnes.FindAsync(id);
            if (personne == null) return NotFound();
            return View(personne);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personne = await _context.Personnes.FindAsync(id);
            if (personne != null)
            {
                _context.Personnes.Remove(personne);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}