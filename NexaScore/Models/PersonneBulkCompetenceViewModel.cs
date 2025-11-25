using System.Collections.Generic;

namespace Projet.Models
{
    public class PersonneBulkCompetenceViewModel
    {
        public int PersonneId { get; set; }
        public List<CompetenceSelectionItem> Competences { get; set; } = new List<CompetenceSelectionItem>();
    }
}