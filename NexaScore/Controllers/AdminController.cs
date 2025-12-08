using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Projet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ProjetContext _context;

        public AdminController(ProjetContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.CountCandidats = await _context.Personnes.CountAsync();
            ViewBag.CountOffres = await _context.Offres.CountAsync();
            ViewBag.CountCompetences = await _context.Competences.CountAsync();
            ViewBag.CountPostes = await _context.Postes.CountAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeedData(int nbCandidats = 30, int nbOffres = 10, int nbPostes = 5, int nbCompetences = 5)
        {
            // Sécurités basiques
            if (nbCandidats < 0) nbCandidats = 0; if (nbCandidats > 500) nbCandidats = 500;
            if (nbOffres < 0) nbOffres = 0; if (nbOffres > 200) nbOffres = 200;
            if (nbPostes < 0) nbPostes = 0; if (nbPostes > 100) nbPostes = 100;
            if (nbCompetences < 0) nbCompetences = 0; if (nbCompetences > 100) nbCompetences = 100;

            // ---------------------------------------------------------------------
            // 1. AJOUT POSTES
            // ---------------------------------------------------------------------
            if (nbPostes > 0)
            {
                var fakerPoste = new Faker<Poste>("fr").RuleFor(p => p.Intitule, f => f.Name.JobTitle());
                var nouveaux = fakerPoste.Generate(nbPostes);
                _context.Postes.AddRange(nouveaux);
                await _context.SaveChangesAsync();
            }
            // Sécurité : toujours avoir au moins 1 poste
            if (!await _context.Postes.AnyAsync())
            {
                _context.Postes.Add(new Poste { Intitule = "Développeur Fullstack" });
                await _context.SaveChangesAsync();
            }

            // ---------------------------------------------------------------------
            // 2. AJOUT COMPÉTENCES
            // ---------------------------------------------------------------------
            if (nbCompetences > 0)
            {
                var fakerComp = new Faker<Competence>("fr")
                    .RuleFor(c => c.Nom, f => $"{f.Hacker.Adjective()}-{f.Hacker.Noun()}".ToUpper());
                var nouvelles = fakerComp.Generate(nbCompetences);
                _context.Competences.AddRange(nouvelles);
                await _context.SaveChangesAsync();
            }
            // Sécurité : toujours avoir au moins 1 compétence
            if (!await _context.Competences.AnyAsync())
            {
                _context.Competences.Add(new Competence { Nom = "C#" });
                await _context.SaveChangesAsync();
            }

            // ---------------------------------------------------------------------
            // CHARGEMENT DES LISTES (CRUCIAL)
            // ---------------------------------------------------------------------
            var dbPostes = await _context.Postes.ToListAsync();
            var dbCompetences = await _context.Competences.ToListAsync();

            // ---------------------------------------------------------------------
            // 3. CANDIDATS
            // ---------------------------------------------------------------------
            if (nbCandidats > 0)
            {
                var fakerPersonne = new Faker<Personne>("fr")
                    .RuleFor(p => p.Nom, f => f.Name.LastName())
                    .RuleFor(p => p.Prenom, f => f.Name.FirstName())
                    .RuleFor(p => p.Email, (f, p) => $"{p.Prenom}.{p.Nom}_{f.Random.AlphaNumeric(5)}@test.com".ToLower())
                    .RuleFor(p => p.Ville, f => f.Address.City())
                    .RuleFor(p => p.CodePostal, f => f.Address.ZipCode())
                    .RuleFor(p => p.Telephone, f => f.Phone.PhoneNumber("06########"))
                    .RuleFor(p => p.DateNaissance, f => DateOnly.FromDateTime(f.Date.Past(40, DateTime.Now.AddYears(-20))))
                    .RuleFor(p => p.AnneesExperienceTotal, f => f.Random.Int(0, 20))
                    // On prend un poste au hasard dans la liste chargée
                    .RuleFor(p => p.TitreJobActuel, f => f.PickRandom(dbPostes).Intitule)
                    .RuleFor(p => p.Description, f => f.Lorem.Sentences(2));

                var nouveauxCandidats = fakerPersonne.Generate(nbCandidats);
                _context.Personnes.AddRange(nouveauxCandidats);
                await _context.SaveChangesAsync();

                // LIAISONS COMPÉTENCES (C'est ici que ça plantait)
                var faker = new Faker();
                var liens = new List<CompetenceAcquise>();

                foreach (var c in nouveauxCandidats)
                {
                    // CORRECTION : On calcule combien on peut en prendre au maximum
                    // On veut entre 2 et 6, MAIS pas plus que le nombre total dispo (dbCompetences.Count)
                    int maxPossible = dbCompetences.Count;
                    int nombreAVouloir = faker.Random.Int(2, 6);
                    int nombreFinal = Math.Min(nombreAVouloir, maxPossible);

                    if (nombreFinal > 0)
                    {
                        var skills = faker.PickRandom(dbCompetences, nombreFinal);
                        foreach (var s in skills)
                        {
                            liens.Add(new CompetenceAcquise { PersonneId = c.Id, CompetenceId = s.Id, Niveau = faker.Random.Int(1, 5) });
                        }
                    }
                }
                _context.CompetenceAcquises.AddRange(liens);
                await _context.SaveChangesAsync();
            }

            // ---------------------------------------------------------------------
            // 4. OFFRES
            // ---------------------------------------------------------------------
            if (nbOffres > 0)
            {
                var fakerOffre = new Faker<Offre>("fr")
                    .RuleFor(o => o.Description, f => f.Lorem.Paragraphs(2))
                    .RuleFor(o => o.VilleCible, f => f.Address.City())
                    .RuleFor(o => o.CodePostalCible, f => f.Address.ZipCode())
                    .RuleFor(o => o.DateCreation, f => f.Date.Recent(30))
                    .RuleFor(o => o.PosteId, f => f.PickRandom(dbPostes).Id);

                var nouvellesOffres = fakerOffre.Generate(nbOffres);

                foreach (var o in nouvellesOffres)
                {
                    var p = dbPostes.First(x => x.Id == o.PosteId);
                    o.Titre = $"{p.Intitule} ({o.VilleCible})";
                }
                _context.Offres.AddRange(nouvellesOffres);
                await _context.SaveChangesAsync();

                // SCORING + SKILLS REQUIS (Sécurisé aussi)
                var faker = new Faker();
                var paramsScoring = new List<ParametreScoring>();
                var skillsRequises = new List<CompetenceSouhaitee>();

                foreach (var o in nouvellesOffres)
                {
                    paramsScoring.Add(new ParametreScoring
                    {
                        OffreId = o.Id,
                        PoidsCompetences = 60,
                        PoidsExperience = 20,
                        PoidsLocalisation = 20,
                        ExclureSiVilleDiff = faker.Random.Bool(0.2f)
                    });

                    // CORRECTION ICI AUSSI
                    int maxPossible = dbCompetences.Count;
                    int nombreAVouloir = faker.Random.Int(3, 5);
                    int nombreFinal = Math.Min(nombreAVouloir, maxPossible);

                    if (nombreFinal > 0)
                    {
                        var reqSkills = faker.PickRandom(dbCompetences, nombreFinal);
                        foreach (var s in reqSkills)
                        {
                            skillsRequises.Add(new CompetenceSouhaitee
                            {
                                OffreId = o.Id,
                                CompetenceId = s.Id,
                                NiveauRequis = faker.Random.Int(2, 4)
                            });
                        }
                    }
                }
                _context.ParametreScorings.AddRange(paramsScoring);
                _context.CompetenceSouhaitees.AddRange(skillsRequises);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Génération réussie avec succès !";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearData()
        {
            _context.CompetenceAcquises.RemoveRange(_context.CompetenceAcquises);
            _context.CompetenceSouhaitees.RemoveRange(_context.CompetenceSouhaitees);
            _context.ParametreScorings.RemoveRange(_context.ParametreScorings);
            _context.Offres.RemoveRange(_context.Offres);
            _context.Personnes.RemoveRange(_context.Personnes);
            _context.Postes.RemoveRange(_context.Postes);
            _context.Competences.RemoveRange(_context.Competences);

            await _context.SaveChangesAsync();
            TempData["Warning"] = "Base de données vidée.";
            return RedirectToAction(nameof(Index));
        }
    }
}