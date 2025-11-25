namespace Projet.Models
{
    // Cette classe est un DTO (Data Transfer Object) comme expliquer précédement.
    public class ActiviteRecente
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string SousTitre { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}