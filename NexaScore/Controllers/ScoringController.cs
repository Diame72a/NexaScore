using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            return View(offres);
        }

        public async Task<IActionResult> Calculer(int id)
        {
            var offre = await _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offre == null) return NotFound();


            int pComp = offre.ParametreScoring?.PoidsCompetences ?? 60;
            int pExp = offre.ParametreScoring?.PoidsExperience ?? 20;
            int pLoc = offre.ParametreScoring?.PoidsLocalisation ?? 20;

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
                    ChartDataCandidat = new List<int>(),
                    ChartDataOffre = new List<int>()
                };

 
                double totalPoints = 0;
                double maxPoints = 0;
                foreach (var cs in offre.CompetenceSouhaitees)
                {
                    maxPoints += 100;
                    vm.ChartLabels.Add(cs.Competence?.Nom ?? "?");
                    int requis = cs.NiveauRequis ?? 1;
                    vm.ChartDataOffre.Add(requis);

                    var ca = candidat.CompetenceAcquises.FirstOrDefault(c => c.CompetenceId == cs.CompetenceId);
                    int reel = ca?.Niveau ?? 0;
                    vm.ChartDataCandidat.Add(reel);

                    totalPoints += (Math.Min((double)reel / requis, 1.1) * 100);
                }
                vm.ScoreCompetences = maxPoints > 0 ? (int)((totalPoints / maxPoints) * 100) : 100;


                int scoreExp = 0;
                int xp = candidat.AnneesExperienceTotal ?? 0;
                var cible = offre.ParametreScoring?.CibleExperience ?? NiveauExperienceCible.PeuImporte;

                switch (cible)
                {
                    case NiveauExperienceCible.Junior:
                        scoreExp = (xp <= 2) ? 100 : 70; break;
                    case NiveauExperienceCible.Confirme:
                        scoreExp = (xp >= 2 && xp <= 5) ? 100 : (xp < 2 ? 40 : 80); break;
                    case NiveauExperienceCible.Senior:
                        scoreExp = (xp >= 5) ? 100 : (int)((double)xp / 5 * 100); break;
                    default: 
                        scoreExp = (xp >= 5) ? 100 : (xp >= 2 ? 80 : 50); break;
                }
                vm.ScoreExperience = scoreExp;


                vm.IsLocalisationOk = (candidat.Ville?.ToLower() == offre.VilleCible?.ToLower());
                vm.ScoreLocalisation = vm.IsLocalisationOk ? 100 : 0;


                double note = (vm.ScoreCompetences * pComp) + (vm.ScoreExperience * pExp) + (vm.ScoreLocalisation * pLoc);
                vm.ScoreGlobal = (int)(note / (pComp + pExp + pLoc));


                if ((offre.ParametreScoring?.ExclureSiVilleDiff ?? false) && !vm.IsLocalisationOk) vm.ScoreGlobal = 0;

                resultats.Add(vm);
            }


            var top5 = resultats.OrderByDescending(r => r.ScoreGlobal).Take(5).ToList();
            ViewBag.TitreOffre = offre.Titre;

            return View(top5);
        }
    }
}