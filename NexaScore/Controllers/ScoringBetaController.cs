using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services;
using System.Diagnostics;

namespace Projet.Controllers
{
    public class ScoringBetaController : Controller
    {
        private readonly ProjetContext _context;
        private readonly IWebHostEnvironment _environment;

        public ScoringBetaController(ProjetContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.OffresCount = await _context.Offres.CountAsync();
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Index(IFormFile cvFile)
        {

            if (cvFile == null || cvFile.Length == 0)
            {
                ViewBag.Error = "Veuillez sélectionner un fichier PDF valide.";
                ViewBag.OffresCount = await _context.Offres.CountAsync();
                return View();
            }


            var uploads = Path.Combine(_environment.WebRootPath, "temp_uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var filePath = Path.Combine(uploads, Guid.NewGuid().ToString() + ".pdf");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }


            var offres = await _context.Offres
                .Include(o => o.Poste)
                .Include(o => o.CompetenceSouhaitees)
                .ThenInclude(cs => cs.Competence)
                .ToListAsync();

            if (!offres.Any())
            {
                ViewBag.Error = "Aucune offre en base pour comparer.";
                try { System.IO.File.Delete(filePath); } catch { }
                return View();
            }

            var resultats = new List<ResultatIA>();
            var scoringService = new PythonScoringService();


            foreach (var offre in offres)
            {

                string texteReference = $"{offre.Titre} {offre.Description}";

                foreach (var skill in offre.CompetenceSouhaitees)
                {
                    string k = skill.Competence.Nom;
                    texteReference += $" {k} {k} {k}";
                }


                var retour = scoringService.CalculerScore(filePath, texteReference);

                if (retour.Success)
                {
                    resultats.Add(new ResultatIA
                    {
                        Offre = offre,
                        Score = retour.Score,
                        MessageIA = retour.Message,

                        MotsCles = retour.Matches ?? new List<string>()
                    });
                }
                else
                {

                    ViewBag.Error = "ERREUR PYTHON : " + retour.Message;
                    try { System.IO.File.Delete(filePath); } catch { }
                    ViewBag.OffresCount = offres.Count;
                    return View();
                }
            }


            try { System.IO.File.Delete(filePath); } catch { }


            var topMatches = resultats.OrderByDescending(x => x.Score).Take(10).ToList();

            return View("Resultats", topMatches);
        }


        public class ResultatIA
        {
            public Offre Offre { get; set; }
            public double Score { get; set; }
            public string MessageIA { get; set; }


            public List<string> MotsCles { get; set; } = new List<string>();
        }
    }
}