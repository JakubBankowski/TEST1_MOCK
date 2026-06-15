namespace TEST1_MOCK.DTOs;

public class CustomerRentalsResponseDto
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public List<RentalResponseDto> rentals { get; set; } = new();
}