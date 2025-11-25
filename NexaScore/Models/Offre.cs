using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class Offre
{
    public int Id { get; set; }

    public string Titre { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? DateCreation { get; set; }

    public string? VilleCible { get; set; }

    public string? CodePostalCible { get; set; }

    public int PosteId { get; set; }

    public virtual ICollection<CompetenceSouhaitee> CompetenceSouhaitees { get; set; } = new List<CompetenceSouhaitee>();

    public virtual ParametreScoring? ParametreScoring { get; set; }

    public virtual Poste Poste { get; set; } = null!;
}
