using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Projet.Controllers
{
    public class PersonnesController : Controller
    {
        private readonly ProjetContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PersonnesController(ProjetContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================
        // 1. INDEX
        // ============================================================
        public async Task<IActionResult> Index(string searchString, string villeFilter)
        {
            var query = _context.Personnes.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Nom.Contains(searchString)
                                      || p.Prenom.Contains(searchString)
                                      || p.Email.Contains(searchString)
                                      || (p.TitreJobActuel != null && p.TitreJobActuel.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(villeFilter))
            {
                query = query.Where(p => p.Ville == villeFilter);
            }

            var candidats = await query.OrderByDescending(p => p.Id).ToListAsync();

            ViewBag.Villes = await _context.Personnes
                .Where(p => p.Ville != null)
                .Select(p => p.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            ViewBag.TotalCandidats = await _context.Personnes.CountAsync();
            ViewBag.TotalExperts = await _context.Personnes.CountAsync(p => (p.AnneesExperienceTotal ?? 0) >= 5);

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentVille"] = villeFilter;

            return View(candidats);
        }

        // ============================================================
        // 2. CREATE
        // ============================================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Personne personne)
        {
            ModelState.Remove("CompetenceAcquises");

            // Validation DateOnly
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (personne.DateNaissance > today)
                ModelState.AddModelError("DateNaissance", "La date de naissance ne peut pas être dans le futur.");

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
        // 3. EDIT (AVEC UPLOAD)
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
        // Ajout des paramètres IFormFile pour les fichiers
        public async Task<IActionResult> Edit(int id, Personne personne,
                                              IFormFile? fileProfil,
                                              IFormFile? fileBanniere,
                                              IFormFile? fileCv)
        {
            if (id != personne.Id) return NotFound();

            ModelState.Remove("CompetenceAcquises");
            // Important : on retire ces champs du binding pour éviter les erreurs de validation
            ModelState.Remove("fileProfil");
            ModelState.Remove("fileBanniere");
            ModelState.Remove("fileCv");

            var today = DateOnly.FromDateTime(DateTime.Now);
            if (personne.DateNaissance > today)
                ModelState.AddModelError("DateNaissance", "Date invalide.");

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.Personnes.FindAsync(id);
                    if (original == null) return NotFound();

                    // --- GESTION UPLOADS ---
                    if (fileProfil != null)
                    {
                        original.ImageProfilPath = await UploadFile(fileProfil, "profils");
                    }
                    if (fileBanniere != null)
                    {
                        original.ImageBannierePath = await UploadFile(fileBanniere, "bannieres");
                    }
                    if (fileCv != null)
                    {
                        original.CvPath = await UploadFile(fileCv, "cvs");
                        original.CvNomFichier = fileCv.FileName;
                    }
                    // -----------------------

                    // Mise à jour des champs texte
                    original.Nom = personne.Nom;
                    original.Prenom = personne.Prenom;
                    original.Email = personne.Email;
                    original.Telephone = personne.Telephone;
                    original.TitreJobActuel = personne.TitreJobActuel;
                    original.Description = personne.Description;
                    original.Ville = personne.Ville;
                    original.CodePostal = personne.CodePostal;
                    original.DateNaissance = personne.DateNaissance;
                    original.AnneesExperienceTotal = personne.AnneesExperienceTotal;

                    _context.Update(original);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Profil et fichiers mis à jour avec succès.";
                    return RedirectToAction(nameof(Edit), new { id = personne.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Personnes.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("Email", "Erreur lors de la sauvegarde.");
                }
            }

            var pComplet = await _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(c => c.Competence)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pComplet != null)
                personne.CompetenceAcquises = pComplet.CompetenceAcquises;

            return View(personne);
        }

        // ============================================================
        // 4. GESTION COMPÉTENCES
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> AjouterPlusieurs(int id)
        {
            var personne = await _context.Personnes.Include(p => p.CompetenceAcquises).FirstOrDefaultAsync(p => p.Id == id);
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
                    NiveauRequis = 2
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
            if (personne != null) { _context.Personnes.Remove(personne); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // HELPER UPLOAD
        // ============================================================
        private async Task<string> UploadFile(IFormFile file, string folderName)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/uploads/" + folderName + "/" + uniqueFileName;
        }
    }
}