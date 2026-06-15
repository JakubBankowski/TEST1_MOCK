namespace TEST1_MOCK.DTOs;

public class RentalResponseDto
{
    public int id { get; set; }
    public DateTime rentalDate { get; set; }
    public DateTime? returnDate { get; set; }
    public string status { get; set; } = null!;
    public List<RentalMoviesResponseDto> movies { get; set; }
}