using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System.Linq;

namespace Projet.Controllers
{
    public class HomeController : Controller
    {
        // Declaration de variable qui va contenir la connexion de la base de données
        // Readonly empeche la modification ou la suppression de la connexion au milieu du code
        private readonly ProjetContext _context;

        // Ici, on injecte la dépendance 
        public HomeController(ProjetContext context)
        {
            _context = context;
        }

        // J'ai utilisé LINQ -> C'est C# qui traduit ces lignes en requêtes SQL
        public IActionResult Index()
        {
            var stats = new HomeDashboardViewModel();

            
            stats.NbOffres = _context.Offres.Count();
            stats.NbCandidats = _context.Personnes.Count();
            stats.NbCompetences = _context.Competences.Count();

            
            var offresParPoste = _context.Offres
                .Include(o => o.Poste)
                .GroupBy(o => o.Poste.Intitule)
                .Select(g => new { Metier = g.Key, Nombre = g.Count() })
                .ToList();

            foreach (var item in offresParPoste)
            {
                stats.LabelsMetiers.Add(item.Metier);
                stats.DataMetiers.Add(item.Nombre);
            }

            
            var candidats = _context.Personnes.ToList();

            int junior = candidats.Count(p => p.AnneesExperienceTotal < 2);
            int confirme = candidats.Count(p => p.AnneesExperienceTotal >= 2 && p.AnneesExperienceTotal < 5);
            int senior = candidats.Count(p => p.AnneesExperienceTotal >= 5);

            stats.DataExperience = new List<int> { junior, confirme, senior };

            
            var dernieresOffres = _context.Offres
                .OrderByDescending(o => o.Id)
                .Take(3)
                .Select(o => new ActiviteRecente
                {
                    Titre = "Nouvelle Offre",
                    SousTitre = o.Titre,
                    Type = "Offre",
                    Id = o.Id
                }).ToList();

            var derniersCandidats = _context.Personnes
                .OrderByDescending(p => p.Id)
                .Take(3)
                .Select(p => new ActiviteRecente
                {
                    Titre = "Nouveau Candidat",
                    SousTitre = $"{p.Prenom} {p.Nom}",
                    Type = "Candidat",
                    Id = p.Id
                }).ToList();

            stats.Activites.AddRange(dernieresOffres);
            stats.Activites.AddRange(derniersCandidats);
            stats.Activites = stats.Activites.OrderByDescending(a => a.Id).Take(5).ToList();

            return View(stats);
        }
    }
}