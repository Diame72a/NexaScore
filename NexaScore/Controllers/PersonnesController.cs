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
    public class PersonnesController : Controller
    {
        private readonly ProjetContext _context;

        public PersonnesController(ProjetContext context)
        {
            _context = context;
        }

        // GET: Personnes
        
        public async Task<IActionResult> Index(string searchString, string villeFilter)
        {
            
            var query = _context.Personnes.AsQueryable();

            
            if (!string.IsNullOrEmpty(searchString))
            {
                
                query = query.Where(p => p.Nom.Contains(searchString)
                                      || p.Prenom.Contains(searchString)
                                      || p.Email.Contains(searchString));
            }

            
            if (!string.IsNullOrEmpty(villeFilter))
            {
                query = query.Where(p => p.Ville == villeFilter);
            }

            
            var candidats = await query.ToListAsync();

            

            
            ViewBag.VillesDisponibles = await _context.Personnes
                                                .Select(p => p.Ville)
                                                .Distinct()
                                                .ToListAsync();

            
            int junior = candidats.Count(p => p.AnneesExperienceTotal < 2);
            int confirme = candidats.Count(p => p.AnneesExperienceTotal >= 2 && p.AnneesExperienceTotal < 5);
            int senior = candidats.Count(p => p.AnneesExperienceTotal >= 5);

            ViewBag.ExpLabels = new List<string> { "Junior", "Confirmé", "Senior" };
            ViewBag.ExpData = new List<int> { junior, confirme, senior };

            
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentVille"] = villeFilter;

            return View(candidats);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            
            var personne = await _context.Personnes
                .Include(p => p.CompetenceAcquises)
                    .ThenInclude(ca => ca.Competence) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (personne == null) return NotFound();

            
            ViewBag.CompetencesDispos = _context.Competences.OrderBy(c => c.Nom).ToList();

            return View(personne);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterCompetence(int personneId, int competenceId, int niveau)
        {
            
            var existeDeja = await _context.CompetenceAcquises
                .AnyAsync(ca => ca.PersonneId == personneId && ca.CompetenceId == competenceId);

            if (!existeDeja)
            {
                var nouvelleComp = new CompetenceAcquise
                {
                    PersonneId = personneId,
                    CompetenceId = competenceId,
                    Niveau = niveau
                };
                _context.Add(nouvelleComp);
                await _context.SaveChangesAsync();
            }

            
            return RedirectToAction(nameof(Details), new { id = personneId });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerCompetence(int id)
        {
            var compAcquise = await _context.CompetenceAcquises.FindAsync(id);
            if (compAcquise != null)
            {
                int personneId = compAcquise.PersonneId; // On garde l'ID pour la redirection
                _context.CompetenceAcquises.Remove(compAcquise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = personneId });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Personnes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Personnes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Prenom,Email,DateNaissance,Ville,CodePostal,AnneesExperienceTotal")] Personne personne)
        {
            if (ModelState.IsValid)
            {
                _context.Add(personne);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(personne);
        }

        // GET: Personnes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personne = await _context.Personnes.FindAsync(id);
            if (personne == null)
            {
                return NotFound();
            }
            return View(personne);
        }

        // POST: Personnes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Prenom,Email,DateNaissance,Ville,CodePostal,AnneesExperienceTotal")] Personne personne)
        {
            if (id != personne.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(personne);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonneExists(personne.Id))
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
            return View(personne);
        }

        // GET: Personnes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personne = await _context.Personnes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (personne == null)
            {
                return NotFound();
            }

            return View(personne);
        }

        // POST: Personnes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personne = await _context.Personnes.FindAsync(id);
            if (personne != null)
            {
                _context.Personnes.Remove(personne);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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
                    var nouvelleAcquisition = new CompetenceAcquise
                    {
                        PersonneId = model.PersonneId,
                        CompetenceId = item.CompetenceId,
                        Niveau = item.NiveauRequis 
                    };
                    _context.Add(nouvelleAcquisition);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Edit), new { id = model.PersonneId });
        }

        private bool PersonneExists(int id)
        {
            return _context.Personnes.Any(e => e.Id == id);
        }
    }
}
