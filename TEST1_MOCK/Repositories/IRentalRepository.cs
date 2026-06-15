using TEST1_MOCK.DTOs;

namespace TEST1_MOCK.Repositories;

public interface IRentalRepository
{
    Task<CustomerDto?> GetCustomerAsync(int id);
    
    Task<CustomerRentalsResponseDto?> GetCustomerRentalsAsync(int id);
    
    Task CreateRentalAsync(int id, CreateRentalRequestDto dto);
    
    Task AddCustomerAsync(CustomerDto dto);

    Task UpdateCustomerAsync(int customerId, CustomerDto dto);
    Task DeleteCustomerAsync(int id);
}