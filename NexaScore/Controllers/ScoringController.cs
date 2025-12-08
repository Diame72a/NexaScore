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
        // 1. INDEX (Liste des offres pour lancer le calcul)
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
        // 2. CALCULER (Cœur du système de Matching)
        // ============================================================
        public IActionResult Calculer(int id)
        {
            // --- 1. CHARGEMENT DES DONNÉES ---
            var offre = _context.Offres
                .Include(o => o.CompetenceSouhaitees).ThenInclude(cs => cs.Competence)
                .Include(o => o.ParametreScoring)
                .FirstOrDefault(o => o.Id == id);

            if (offre == null) return NotFound();

            // Poids par défaut si non définis
            int poidsComp = offre.ParametreScoring?.PoidsCompetences ?? 60;
            int poidsExp = offre.ParametreScoring?.PoidsExperience ?? 20;
            int poidsLoc = offre.ParametreScoring?.PoidsLocalisation ?? 20;

            var candidats = _context.Personnes
                .Include(p => p.CompetenceAcquises).ThenInclude(ca => ca.Competence)
                .ToList();

            var resultats = new List<CandidateMatchViewModel>();

            // --- 2. BOUCLE SUR CHAQUE CANDIDAT ---
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

                // ==================================================
                // A. SCORE COMPÉTENCES
                // ==================================================
                double totalPointsCompetences = 0;
                double maxPointsCompetences = 0;

                foreach (var compRequise in offre.CompetenceSouhaitees)
                {
                    maxPointsCompetences += 100;
                    vm.ChartLabels.Add(compRequise.Competence?.Nom ?? "?");

                    int niveauRequisInt = compRequise.NiveauRequis ?? 1;
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
                            ratio = 1.1; // Petit bonus pour l'expertise
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

                // Calcul score compétences sur 100
                double scoreBrutComp = (maxPointsCompetences > 0)
                    ? (totalPointsCompetences / maxPointsCompetences) * 100
                    : 100;

                vm.ScoreCompetences = (int)scoreBrutComp;


                // ==================================================
                // B. SCORE EXPÉRIENCE (LOGIQUE FOURCHETTE 0-2 ans)
                // ==================================================
                int scoreBrutExp = 0;
                int xpCandidat = candidat.AnneesExperienceTotal ?? 0;

                // Récupération de l'objectif (Enum)
                var niveauCible = offre.ParametreScoring?.CibleExperience ?? NiveauExperienceCible.PeuImporte;

                int minRequis = 0;
                int maxRequis = 0;
                bool checkSurqualification = false;

                // Configuration des bornes selon le profil
                switch (niveauCible)
                {
                    case NiveauExperienceCible.Junior:
                        minRequis = 0;  // Accepte débutant complet
                        maxRequis = 2;  // Jusqu'à 2 ans inclus (0, 1, 2 = 100%)
                        checkSurqualification = true; // Activer le piège à Seniors
                        break;

                    case NiveauExperienceCible.Confirme:
                        minRequis = 3;
                        maxRequis = 5;
                        break;

                    case NiveauExperienceCible.Senior:
                        minRequis = 6;
                        maxRequis = 99; // Pas de limite haute
                        break;

                    default: // PeuImporte
                        minRequis = 0;
                        maxRequis = 99;
                        break;
                }

                // Calcul mathématique
                if (niveauCible == NiveauExperienceCible.PeuImporte)
                {
                    // Ancienne logique (Plus c'est mieux)
                    if (xpCandidat >= 5) scoreBrutExp = 100;
                    else if (xpCandidat >= 2) scoreBrutExp = 70;
                    else scoreBrutExp = 40;
                }
                else
                {
                    // --- CAS 1 : DANS LA CIBLE (MATCH PARFAIT) ---
                    // Ex: Junior (0-2) et Candidat a 0, 1 ou 2 ans -> 100%
                    if (xpCandidat >= minRequis && xpCandidat <= maxRequis)
                    {
                        scoreBrutExp = 100;
                        vm.DetailsPositifs.Add($"Expérience idéale ({xpCandidat} ans)");
                    }
                    // --- CAS 2 : SOUS-QUALIFIÉ ---
                    // Ex: Confirmé (min 3) et Candidat a 1 an
                    else if (xpCandidat < minRequis)
                    {
                        double denominateur = (minRequis == 0) ? 1 : (double)minRequis;
                        double ratio = (double)xpCandidat / denominateur;

                        scoreBrutExp = (int)(ratio * 100);
                        vm.DetailsNegatifs.Add($"Manque d'expérience ({xpCandidat} ans / {minRequis} min)");
                    }
                    // --- CAS 3 : SUR-QUALIFIÉ (Au dessus du max) ---
                    else
                    {
                        int ecart = xpCandidat - maxRequis;

                        // Si Junior (max 2) et candidat a beaucoup plus (écart >= 3, donc 5+ ans)
                        if (checkSurqualification && ecart >= 3)
                        {
                            scoreBrutExp = 55; // Pénalité
                            vm.DetailsNegatifs.Add($"⚠️ Surqualifié pour un poste Junior ({xpCandidat} ans)");
                        }
                        else
                        {
                            // Pour les autres profils, avoir plus est un bonus
                            scoreBrutExp = 100;
                            vm.DetailsPositifs.Add($"Expérience solide ({xpCandidat} ans)");
                        }
                    }
                }

                vm.ScoreExperience = scoreBrutExp;


                // ==================================================
                // C. SCORE LOCALISATION
                // ==================================================
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


                // ==================================================
                // D. TOTAL PONDÉRÉ
                // ==================================================
                double noteFinale = (scoreBrutComp * poidsComp) + (scoreBrutExp * poidsExp) + (scoreBrutLoc * poidsLoc);
                double sommePoids = poidsComp + poidsExp + poidsLoc;
                if (sommePoids == 0) sommePoids = 1;

                vm.ScoreGlobal = (int)(noteFinale / sommePoids);


                // ==================================================
                // E. EXCLUSION (KILLER QUESTION)
                // ==================================================
                bool fautExclure = offre.ParametreScoring?.ExclureSiVilleDiff ?? false;

                if (fautExclure && scoreBrutLoc == 0)
                {
                    vm.ScoreGlobal = 0;
                    vm.DetailsNegatifs.Insert(0, "⛔ DISQUALIFIÉ (Mauvaise Ville)");
                }

                resultats.Add(vm);
            }

            // --- 3. TRI ET RETOUR VUE ---
            var classement = resultats.OrderByDescending(r => r.ScoreGlobal).ToList();
            ViewBag.TitreOffre = offre.Titre;

            return View(classement);
        }
    }
}