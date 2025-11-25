using System.Collections.Generic;

namespace Projet.Models
{
    public class CandidateMatchViewModel
    {
        
        public int CandidatId { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string JobActuel { get; set; } = string.Empty;

        // --- LES SCORES ---
        public int ScoreGlobal { get; set; } 

        public int ScoreCompetences { get; set; }
        public int ScoreExperience { get; set; }
        public int ScoreLocalisation { get; set; }
        
        public List<string> DetailsPositifs { get; set; } = new List<string>();

        public List<string> DetailsNegatifs { get; set; } = new List<string>();

        public bool IsLocalisationOk { get; set; }
    }
}  