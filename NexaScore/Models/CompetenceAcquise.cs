using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class CompetenceAcquise
{
    public int Id { get; set; }

    public int PersonneId { get; set; }

    public int CompetenceId { get; set; }

    public int? Niveau { get; set; }

    public virtual Competence Competence { get; set; } = null!;

    public virtual Personne Personne { get; set; } = null!;
}
