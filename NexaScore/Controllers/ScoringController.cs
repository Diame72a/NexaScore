using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Projet.Controllers
{
    public class ScoringController : Controller
    {
        private readonly ProjetContext _context;

        public ScoringController(ProjetContext context)
        {
            _context = context;
        }


        public IActionResult Index()
        {

            var offres = _context.Offres.Include(o => o.Poste).ToList();
            return View(offres);
        }


        public IActionResult Calculer(int id)
        {
            
            var offre = _context.Offres
                .Include(o => o.CompetenceSouhaitees)
                    .ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefault(o => o.Id == id);

            if (offre == null) return NotFound();

            
            int poidsComp = offre.ParametreScoring?.PoidsCompetences ?? 60;
            int poidsExp = offre.ParametreScoring?.PoidsExperience ?? 20;
            int poidsLoc = offre.ParametreScoring?.PoidsLocalisation ?? 20;

            
            var candidats = _context.Personnes
                .Include(p => p.CompetenceAcquises)
                    .ThenInclude(ca => ca.Competence) 
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

                
                double totalPointsCompetences = 0;
                double maxPointsCompetences = 0;

                foreach (var compRequise in offre.CompetenceSouhaitees)
                {
                    maxPointsCompetences += 100;

                    var compCandidat = candidat.CompetenceAcquises
                        .FirstOrDefault(c => c.CompetenceId == compRequise.CompetenceId);

                    if (compCandidat != null)
                    {
                        
                        double niveauCandidat = (double)(compCandidat.Niveau ?? 0);
                        double niveauRequis = (double)(compRequise.NiveauRequis ?? 1); 

                        double ratio = niveauCandidat / niveauRequis;

                        if (ratio > 1) ratio = 1;

                        totalPointsCompetences += (ratio * 100);

                        if (ratio >= 1)
                            vm.DetailsPositifs.Add($"{compRequise.Competence?.Nom ?? "Inconnu"} (Niveau OK)");
                        else
                            vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom ?? "Inconnu"} (Niveau faible)");
                    }
                    else
                    {
                        vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom ?? "Inconnu"} (Manquante)");
                    }
                }

                
                double scoreBrutComp = (maxPointsCompetences > 0)
                    ? (totalPointsCompetences / maxPointsCompetences) * 100
                    : 0;

                vm.ScoreCompetences = (int)scoreBrutComp;


                int scoreBrutExp = 0;

                int xpCandidat = candidat.AnneesExperienceTotal ?? 0; 

                if (xpCandidat >= 5) scoreBrutExp = 100;
                else if (xpCandidat >= 2) scoreBrutExp = 70;
                else scoreBrutExp = 30;

                vm.ScoreExperience = scoreBrutExp;


                int scoreBrutLoc = 0;
                string villeOffre = offre.VilleCible?.Trim().ToLower() ?? "";
                string villeCandidat = candidat.Ville?.Trim().ToLower() ?? "";

                if (!string.IsNullOrEmpty(villeOffre) && !string.IsNullOrEmpty(villeCandidat))
                {
                    if (villeOffre == villeCandidat)
                    {
                        scoreBrutLoc = 100;
                        vm.IsLocalisationOk = true;
                        vm.DetailsPositifs.Add("Même ville");
                    }
                    else
                    {
                        vm.DetailsNegatifs.Add($"Ville différente ({candidat.Ville})");
                    }
                }
                vm.ScoreLocalisation = scoreBrutLoc;


                double noteFinale = (scoreBrutComp * poidsComp) + (scoreBrutExp * poidsExp) + (scoreBrutLoc * poidsLoc);


                double sommePoids = poidsComp + poidsExp + poidsLoc;
                if (sommePoids == 0) sommePoids = 1;

                vm.ScoreGlobal = (int)(noteFinale / sommePoids);


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