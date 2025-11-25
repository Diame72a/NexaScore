using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class ParametreScoring
{
    public int Id { get; set; }

    public int OffreId { get; set; }

    public int? PoidsCompetences { get; set; }

    public int? PoidsExperience { get; set; }

    public int? PoidsLocalisation { get; set; }

    public bool? ExclureSiVilleDiff { get; set; }

    public bool? ExclureSiExperienceManquante { get; set; }

    public virtual Offre Offre { get; set; } = null!;
}
