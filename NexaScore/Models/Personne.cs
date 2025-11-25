using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class Personne
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;


    public string Email { get; set; } = null!;

    public DateOnly DateNaissance { get; set; }

    public string? Ville { get; set; }

    public string? CodePostal { get; set; }

    public int? AnneesExperienceTotal { get; set; }

    public virtual ICollection<CompetenceAcquise> CompetenceAcquises { get; set; } = new List<CompetenceAcquise>();
}
