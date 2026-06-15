using System.ComponentModel.DataAnnotations;

namespace TEST1_MOCK.DTOs;

public class CustomerDto
{
    public int Id { get; set; }

    [Required] 
    public string FirstName { get; set; } = null!;
    
    [Required]
    public string LastName { get; set; } = null!;
}