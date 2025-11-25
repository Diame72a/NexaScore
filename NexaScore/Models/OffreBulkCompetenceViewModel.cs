using System.Collections.Generic;

namespace Projet.Models
{
    public class OffreBulkCompetenceViewModel
    {
        public int OffreId { get; set; }
        public List<CompetenceSelectionItem> Competences { get; set; } = new List<CompetenceSelectionItem>();
    }

    public class CompetenceSelectionItem
    {
        public int CompetenceId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public bool EstSelectionne { get; set; }
        public int NiveauRequis { get; set; } = 3;
    }
}