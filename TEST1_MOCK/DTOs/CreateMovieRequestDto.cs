using System.ComponentModel.DataAnnotations;

namespace TEST1_MOCK.DTOs;

public class CreateMovieRequestDto
{
    [Required]
    public string title { get; set; }
    
    [Required]
    [Range(0.0, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal rentalPrice  { get; set; }
}