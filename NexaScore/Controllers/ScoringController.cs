using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services; 
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Projet.Controllers
{
    [Authorize]
    public class ScoringController : Controller
    {
        private readonly ProjetContext _context;
        private readonly INotificationService _notifService; 


        public ScoringController(ProjetContext context, INotificationService notifService)
        {
            _context = context;
            _notifService = notifService;
        }


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


        public async Task<IActionResult> Calculer(int id)
        {

            var offre = await _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null) return NotFound();


            int poidsComp = offre.ParametreScoring?.PoidsCompetences ?? 60;
            int poidsExp = offre.ParametreScoring?.PoidsExperience ?? 20;
            int poidsLoc = offre.ParametreScoring?.PoidsLocalisation ?? 20;

            var candidats = await _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(ca => ca.Competence)
                .ToListAsync();

            var resultats = new List<CandidateMatchViewModel>();


            foreach (var candidat in candidats)
            {
                var vm = new CandidateMatchViewModel
                {
                    CandidatId = candidat.Id,
                    NomComplet = $"{candidat.Prenom} {candidat.Nom}",
                    JobActuel = candidat.Ville ?? "Ville inconnue",
                    ChartLabels = new List<string>(),
                    ChartDataOffre = new List<int>(),
                    ChartDataCandidat = new List<int>(),
                    DetailsPositifs = new List<string>(),
                    DetailsNegatifs = new List<string>()
                };

                
                double totalPointsCompetences = 0;
                double maxPointsCompetences = 0;

                foreach (var compRequise in offre.CompetenceSouhaitees)
                {
                    maxPointsCompetences += 100;
                    vm.ChartLabels.Add(compRequise.Competence?.Nom ?? "?");

                    int niveauRequisInt = compRequise.NiveauRequis ?? 0;
                    vm.ChartDataOffre.Add(niveauRequisInt);

                    var compCandidat = candidat.CompetenceAcquises
                        .FirstOrDefault(c => c.CompetenceId == compRequise.CompetenceId);

                    if (compCandidat != null)
                    {
                        int niveauReel = compCandidat.Niveau ?? 0;
                        vm.ChartDataCandidat.Add(niveauReel);

                        double ratio = (double)niveauReel / (double)niveauRequisInt;

                        if (ratio > 1)
                        {
                            ratio = 1.1; 
                            vm.DetailsPositifs.Add($"{compRequise.Competence?.Nom} (Expertise sup.)");
                        }
                        else if (ratio >= 1)
                        {
                            ratio = 1.0;
                            vm.DetailsPositifs.Add($"{compRequise.Competence?.Nom} (Acquis)");
                        }
                        else
                        {
                            vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom} (Faible)");
                        }

                        totalPointsCompetences += (ratio * 100);
                    }
                    else
                    {
                        vm.ChartDataCandidat.Add(0);
                        vm.DetailsNegatifs.Add($"{compRequise.Competence?.Nom} (Manquante)");
                    }
                }

                
                double scoreBrutComp = (maxPointsCompetences > 0)
                    ? (totalPointsCompetences / maxPointsCompetences) * 100
                    : 100;

                vm.ScoreCompetences = (int)scoreBrutComp;

                
                int scoreBrutExp = 0;
                int xpCandidat = candidat.AnneesExperienceTotal ?? 0;
                var niveauCible = offre.ParametreScoring?.CibleExperience ?? NiveauExperienceCible.PeuImporte;

                int minRequis = 0;
                int maxRequis = 0;
                bool checkSurqualification = false;

                switch (niveauCible)
                {
                    case NiveauExperienceCible.Junior:
                        minRequis = 0; maxRequis = 2; checkSurqualification = true;
                        break;
                    case NiveauExperienceCible.Confirme:
                        minRequis = 3; maxRequis = 5;
                        break;
                    case NiveauExperienceCible.Senior:
                        minRequis = 6; maxRequis = 99;
                        break;
                    default:
                        minRequis = 0; maxRequis = 99;
                        break;
                }

                if (niveauCible == NiveauExperienceCible.PeuImporte)
                {
                    if (xpCandidat >= 5) scoreBrutExp = 100;
                    else if (xpCandidat >= 2) scoreBrutExp = 70;
                    else scoreBrutExp = 40;
                }
                else
                {
                    if (xpCandidat >= minRequis && xpCandidat <= maxRequis)
                    {
                        scoreBrutExp = 100;
                        vm.DetailsPositifs.Add($"Expérience idéale ({xpCandidat} ans)");
                    }
                    else if (xpCandidat < minRequis)
                    {
                        double denominateur = (minRequis == 0) ? 1 : (double)minRequis;
                        double ratio = (double)xpCandidat / denominateur;
                        scoreBrutExp = (int)(ratio * 100);
                        vm.DetailsNegatifs.Add($"Manque d'expérience ({xpCandidat} ans / {minRequis} min)");
                    }
                    else
                    {
                        int ecart = xpCandidat - maxRequis;
                        if (checkSurqualification && ecart >= 3)
                        {
                            scoreBrutExp = 55; 
                            vm.DetailsNegatifs.Add($"⚠️ Surqualifié pour un poste Junior ({xpCandidat} ans)");
                        }
                        else
                        {
                            scoreBrutExp = 100;
                            vm.DetailsPositifs.Add($"Expérience solide ({xpCandidat} ans)");
                        }
                    }
                }
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
                        vm.DetailsPositifs.Add("Localisation idéale");
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

            
            int nbTop = classement.Count(c => c.ScoreGlobal >= 70);

            await _notifService.Ajouter(
                "Analyse Terminée",
                $"Le scoring pour '{offre.Titre}' a généré {nbTop} profils prometteurs.",
                "fas fa-magic",
                "text-warning", 
                Url.Action("Calculer", "Scoring", new { id = offre.Id })
            );
            

            return View(classement);
        }
    }
}