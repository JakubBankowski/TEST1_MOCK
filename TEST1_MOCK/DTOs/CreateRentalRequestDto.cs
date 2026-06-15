using System.ComponentModel.DataAnnotations;

namespace TEST1_MOCK.DTOs;

public class CreateRentalRequestDto
{
    [Required]
    public int id { get; set; }
    
    [Required]
    public DateTime rentalDate { get; set; }
    
    public List<CreateMovieRequestDto> movies { get; set; } = new();
}