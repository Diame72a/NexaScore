using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class Competence
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<CompetenceAcquise> CompetenceAcquises { get; set; } = new List<CompetenceAcquise>();

    public virtual ICollection<CompetenceSouhaitee> CompetenceSouhaitees { get; set; } = new List<CompetenceSouhaitee>();
}
