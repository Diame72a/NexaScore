using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projet.Controllers
{
    public class ScoringController : Controller
    {
        private readonly ProjetContext _context;

        public ScoringController(ProjetContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. INDEX
        // ============================================================
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Offres.Include(o => o.Poste).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(o => o.Titre.Contains(searchString) || o.VilleCible.Contains(searchString));
            }

            var offres = await query.OrderByDescending(o => o.DateCreation).ToListAsync();
            ViewBag.TotalCandidats = await _context.Personnes.CountAsync();
            ViewData["CurrentFilter"] = searchString;

            return View(offres);
        }

        // ============================================================
        // 2. CALCULER (CORRECTION TYPE INT?)
        // ============================================================
        public IActionResult Calculer(int id)
        {
            var offre = _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefault(o => o.Id == id);

            if (offre == null) return NotFound();

            int poidsComp = offre.ParametreScoring?.PoidsCompetences ?? 60;
            int poidsExp = offre.ParametreScoring?.PoidsExperience ?? 20;
            int poidsLoc = offre.ParametreScoring?.PoidsLocalisation ?? 20;

            var candidats = _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(ca => ca.Competence)
                .ToList();

            var resultats = new List<CandidateMatchViewModel>();

            foreach (var candidat in candidats)
            {
                var vm = new CandidateMatchViewModel
                {
                    CandidatId = candidat.Id,
                    NomComplet = $"{candidat.Prenom} {candidat.Nom}",
                    JobActuel = candidat.Ville ?? "Ville inconnue"
                };

                // --- A. SCORE COMPÉTENCES ---
                double totalPointsCompetences = 0;
                double maxPointsCompetences = 0;

                foreach (var compRequise in offre.CompetenceSouhaitees)
                {
                    maxPointsCompetences += 100;

                    vm.ChartLabels.Add(compRequise.Competence?.Nom ?? "?");

                    // --- CORRECTION CRITIQUE ICI (Ligne ~80) ---
                    // On force la conversion : Si c'est null, ça vaut 1.
                    int niveauRequisInt = compRequise.NiveauRequis ?? 1;
                    vm.ChartDataOffre.Add(niveauRequisInt);

                    var compCandidat = candidat.CompetenceAcquises
                        .FirstOrDefault(c => c.CompetenceId == compRequise.CompetenceId);

                    if (compCandidat != null)
                    {
                        // Sécurité sur le niveau du candidat
                        int niveauReel = compCandidat.Niveau ?? 0;

                        vm.ChartDataCandidat.Add(niveauReel);

                        double niveauC = (double)niveauReel;
                        double niveauR = (double)niveauRequisInt;
                        double ratio = niveauC / niveauR;

                        if (ratio > 1)
                        {
                            ratio = 1.1;
                            vm.DetailsPositifs.Add($"{compRequise.Competence?.Nom} (Expertise supérieure !)");
                        }
                        else if (ratio >= 1)
                        {
                            ratio = 1.0;
                            vm.DetailsPositifs.Add($"{compRequise.Competence?.Nom} (Niveau atteint)");
                        }
                        else
                        {
                            vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom} (Niveau faible)");
                        }

                        totalPointsCompetences += (ratio * 100);
                    }
                    else
                    {
                        vm.ChartDataCandidat.Add(0);
                        vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom} (Manquante)");
                    }
                }

                double scoreBrutComp = (maxPointsCompetences > 0) ? (totalPointsCompetences / maxPointsCompetences) * 100 : 100;
                vm.ScoreCompetences = (int)scoreBrutComp;


                // --- B. SCORE EXPÉRIENCE ---
                int scoreBrutExp = 0;
                // Sécurité sur l'expérience
                int xpCandidat = candidat.AnneesExperienceTotal ?? 0;

                if (xpCandidat >= 5) scoreBrutExp = 100;
                else if (xpCandidat >= 2) scoreBrutExp = 70;
                else scoreBrutExp = 40;

                vm.ScoreExperience = scoreBrutExp;


                // --- C. SCORE LOCALISATION ---
                int scoreBrutLoc = 0;
                string villeOffre = offre.VilleCible?.Trim().ToLower() ?? "";
                string villeCandidat = candidat.Ville?.Trim().ToLower() ?? "";

                if (!string.IsNullOrEmpty(villeOffre) && !string.IsNullOrEmpty(villeCandidat))
                {
                    if (villeOffre == villeCandidat)
                    {
                        scoreBrutLoc = 100;
                        vm.IsLocalisationOk = true;
                        vm.DetailsPositifs.Add("Localisation idéale");
                    }
                    else
                    {
                        vm.DetailsNegatifs.Add($"Ville différente ({candidat.Ville})");
                    }
                }
                vm.ScoreLocalisation = scoreBrutLoc;

                // --- D. TOTAL ---
                double noteFinale = (scoreBrutComp * poidsComp) + (scoreBrutExp * poidsExp) + (scoreBrutLoc * poidsLoc);
                double sommePoids = poidsComp + poidsExp + poidsLoc;
                if (sommePoids == 0) sommePoids = 1;

                vm.ScoreGlobal = (int)(noteFinale / sommePoids);

                // --- E. EXCLUSION ---
                bool fautExclure = offre.ParametreScoring?.ExclureSiVilleDiff ?? false;
                if (fautExclure && scoreBrutLoc == 0)
                {
                    vm.ScoreGlobal = 0;
                    vm.DetailsNegatifs.Insert(0, "⛔ DISQUALIFIÉ (Mauvaise Ville)");
                }

                resultats.Add(vm);
            }

            var classement = resultats.OrderByDescending(r => r.ScoreGlobal).ToList();
            ViewBag.TitreOffre = offre.Titre;

            return View(classement);
        }
    }
}