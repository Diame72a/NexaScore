using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class CompetenceSouhaitee
{
    public int Id { get; set; }

    public int OffreId { get; set; }

    public int CompetenceId { get; set; }

    public int? NiveauRequis { get; set; }

    public virtual Competence Competence { get; set; } = null!;

    public virtual Offre Offre { get; set; } = null!;
}
