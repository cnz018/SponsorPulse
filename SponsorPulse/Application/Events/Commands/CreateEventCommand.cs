using System.ComponentModel.DataAnnotations;

namespace SponsorPulse.Application.Events.Commands;

public class CreateEventCommand
{
    [Required(ErrorMessage = "Le nom de l'événement est obligatoire.")]
    [StringLength(100, ErrorMessage = "Le nom est trop long (100 caractères max).")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Une description est requise.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date est obligatoire.")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Veuillez sélectionner une plateforme.")]
    public string StreamPlatform { get; set; } = "Twitch";

    [Required(ErrorMessage = "L'ID de la chaîne est requis.")]
    public string ChannelId { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Les hashtags ne peuvent pas dépasser 500 caractères.")]
    public string? TwitterHashtags { get; set; }
}
