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
        // 1. INDEX (LISTE AVEC RECHERCHE ET FILTRES)
        // ============================================================
        public async Task<IActionResult> Index(string searchString, string villeFilter)
        {
            var query = _context.Personnes.AsQueryable();

            // Filtre par Texte (Nom, Prénom ou Email)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Nom.Contains(searchString)
                                      || p.Prenom.Contains(searchString)
                                      || p.Email.Contains(searchString));
            }

            // Filtre par Ville
            if (!string.IsNullOrEmpty(villeFilter))
            {
                query = query.Where(p => p.Ville == villeFilter);
            }

            // Récupération des données triées par ID décroissant (les plus récents en premier)
            var candidats = await query.OrderByDescending(p => p.Id).ToListAsync();

            // --- DONNÉES POUR LES FILTRES ET GRAPHIQUES ---

            // Liste des villes uniques pour le dropdown
            ViewBag.Villes = await _context.Personnes
                                           .Select(p => p.Ville)
                                           .Distinct()
                                           .OrderBy(v => v)
                                           .ToListAsync();

            // Stats pour le graphique "Expérience"
            int junior = candidats.Count(p => p.AnneesExperienceTotal < 2);
            int confirme = candidats.Count(p => p.AnneesExperienceTotal >= 2 && p.AnneesExperienceTotal < 5);
            int senior = candidats.Count(p => p.AnneesExperienceTotal >= 5);

            ViewBag.ExpLabels = new List<string> { "Junior", "Confirmé", "Senior" };
            ViewBag.ExpData = new List<int> { junior, confirme, senior };

            // On renvoie les filtres actuels à la vue
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
            // On ignore la validation des compétences car elles sont ajoutées à l'étape 2
            ModelState.Remove("CompetenceAcquises");

            // VALIDATION DATE (DateOnly vs DateTime)
            var dateDuJour = DateOnly.FromDateTime(DateTime.Now);

            if (personne.DateNaissance > dateDuJour)
                ModelState.AddModelError("DateNaissance", "La date de naissance ne peut pas être dans le futur.");

            if (personne.DateNaissance.Year < 1900)
                ModelState.AddModelError("DateNaissance", "Année de naissance invalide (trop ancienne).");

            if (ModelState.IsValid)
            {
                _context.Add(personne);
                await _context.SaveChangesAsync();

                // Redirection vers l'Étape 2 (Edit) avec message de succès
                TempData["SuccessMessage"] = "Profil créé avec succès ! Ajoutez maintenant les compétences.";
                return RedirectToAction(nameof(Edit), new { id = personne.Id });
            }
            return View(personne);
        }

        // ============================================================
        // 3. EDIT (MODIFICATION ET GESTION COMPÉTENCES)
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // IMPORTANT : On charge la personne ET ses compétences associées
            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises)
                    .ThenInclude(ca => ca.Competence) // Pour afficher le nom "Java" et pas juste l'ID
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personne == null) return NotFound();

            return View(personne);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Personne personne)
        {
            if (id != personne.Id) return NotFound();

            // On ignore la validation de la liste qui n'est pas envoyée par le formulaire principal
            ModelState.Remove("CompetenceAcquises");

            // VALIDATION DATE
            var dateDuJour = DateOnly.FromDateTime(DateTime.Now);
            if (personne.DateNaissance > dateDuJour)
                ModelState.AddModelError("DateNaissance", "La date ne peut pas être dans le futur.");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. On récupère l'objet original en base pour éviter les conflits
                    var original = await _context.Personnes.FindAsync(id);
                    if (original == null) return NotFound();

                    // 2. On met à jour manuellement les champs modifiables
                    original.Nom = personne.Nom;
                    original.Prenom = personne.Prenom;
                    original.Email = personne.Email;
                    original.Ville = personne.Ville;
                    original.CodePostal = personne.CodePostal;
                    original.DateNaissance = personne.DateNaissance;
                    original.AnneesExperienceTotal = personne.AnneesExperienceTotal;

                    // 3. On sauvegarde
                    _context.Update(original);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Personnes.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("Email", "Cet email semble déjà exister.");
                }
            }

            // En cas d'erreur, on doit recharger les compétences pour l'affichage
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

        // Affiche la modale avec les compétences non encore possédées
        [HttpGet]
        public async Task<IActionResult> AjouterPlusieurs(int id)
        {
            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personne == null) return NotFound();

            // On filtre pour ne proposer que ce qu'il n'a pas encore
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

        // Traite le formulaire de la modale
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
                        Niveau = item.NiveauRequis // Ici "NiveauRequis" sert de stockage pour le Niveau réel
                    });
                }
                await _context.SaveChangesAsync();
            }
            // On redirige vers Edit pour voir les changements
            return RedirectToAction(nameof(Edit), new { id = model.PersonneId });
        }

        // Supprime une compétence
        [HttpPost]
        public async Task<IActionResult> SupprimerCompetence(int id, string source)
        {
            var comp = await _context.CompetenceAcquises.FindAsync(id);
            if (comp != null)
            {
                int pid = comp.PersonneId;
                _context.CompetenceAcquises.Remove(comp);
                await _context.SaveChangesAsync();

                // Redirection intelligente selon d'où on vient
                if (source == "Edit") return RedirectToAction(nameof(Edit), new { id = pid });
                return RedirectToAction(nameof(Details), new { id = pid });
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 5. DETAILS & DELETE (STANDARD)
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