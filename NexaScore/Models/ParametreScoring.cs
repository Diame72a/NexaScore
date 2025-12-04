using System.ComponentModel.DataAnnotations;

namespace Projet.Models
{
    // 1. On définit les choix possibles pour le recruteur
    public enum NiveauExperienceCible
    {
        [Display(Name = "Peu importe (Plus c'est mieux)")]
        PeuImporte = 0, // Comportement actuel par défaut

        [Display(Name = "Junior (0-2 ans)")]
        Junior = 1,

        [Display(Name = "Confirmé (2-5 ans)")]
        Confirme = 2,

        [Display(Name = "Senior / Expert (+5 ans)")]
        Senior = 3
    }

    public class ParametreScoring
    {
        public int Id { get; set; }
        public int OffreId { get; set; }


        public virtual Offre? Offre { get; set; }


        [Range(0, 100)]
        public int PoidsCompetences { get; set; } = 60;

        [Range(0, 100)]
        public int PoidsExperience { get; set; } = 20;

        [Range(0, 100)]
        public int PoidsLocalisation { get; set; } = 20;


        public bool? ExclureSiExperienceManquante { get; set; }

        public bool ExclureSiVilleDiff { get; set; } = false;


        public NiveauExperienceCible CibleExperience { get; set; } = NiveauExperienceCible.PeuImporte;
    }
}