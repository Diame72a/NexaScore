namespace Projet.Models
{
    public class HomeDashboardViewModel
    {

// Ici j'ai créé une class DTO ( Data Transfer Object )
// Cette class contient que ce dont la page a besoin ( 3 chiffre entiers ) 
        public int NbOffres { get; set; }
        public int NbCandidats { get; set; }
        public int NbCompetences { get; set; }

        public List<string> LabelsMetiers { get; set; } = new List<string>();
        public List<int> DataMetiers { get; set; } = new List<int>();

        public List<int> DataExperience { get; set; } = new List<int>();

        public List<ActiviteRecente> Activites { get; set; } = new List<ActiviteRecente>();
    }
}
