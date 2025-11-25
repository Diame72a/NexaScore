using System;
using System.Collections.Generic;

namespace Projet.Models;

public partial class Poste
{
    public int Id { get; set; }

    public string Intitule { get; set; } = null!;

    public virtual ICollection<Offre> Offres { get; set; } = new List<Offre>();
}
