using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TEST1_MOCK.DTOs;

namespace TEST1_MOCK.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly string _connectionString;
    
    public CustomersController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(
            "SELECT customer_id, first_name, last_name FROM Customer WHERE customer_id = @customerId;",
            connection);
        command.Parameters.AddWithValue("@customerId", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound();

        var customer = new CustomerDto
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2)
        };
        
        return Ok(customer);
    }

    [HttpGet("{id}/rentals")]
    public async Task<IActionResult> GetCustomerRentals(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string firstName;
        string lastName;

        await using (var customerCommand = new SqlCommand(@"
                                                          SELECT first_name, last_name FROM Customer WHERE customer_id = @customerId;", connection))
                                                          
        {
            customerCommand.Parameters.AddWithValue("@customerId", id);
            
            await using var customerReader = await customerCommand.ExecuteReaderAsync();
            if (!await customerReader.ReadAsync())
            {
                return NotFound();
            }
            
            firstName = customerReader.GetString(0);
            lastName = customerReader.GetString(1);
        }

        var rentalsById = new Dictionary<int, RentalResponseDto>();

        await using (var rentalsCommand = new SqlCommand(@"
                                                         SELECT r.rental_id,
                                                         r.rental_date,
                                                         r.return_date,
                                                         s.name AS status,
                                                         m.title AS title,
                                                         ri.price_at_rental
                                                         FROM Rental r
                                                         JOIN Status s ON s.status_id = r.status_id
                                                         LEFT JOIN Rental_Item ri ON ri.rental_id = r.rental_id
                                                         LEFT JOIN Movie m ON m.movie_id = ri.movie_id
                                                         WHERE r.customer_id = @customerId
                                                         ORDER BY r.rental_id, m.title;", connection))
        {
            rentalsCommand.Parameters.AddWithValue("@customerId", id);

            await using var reader = await rentalsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var rentalId = reader.GetInt32(0);

                if (!rentalsById.TryGetValue(rentalId, out var rental))
                {
                    rental = new RentalResponseDto
                    {
                        id = rentalId,
                        rentalDate = reader.GetDateTime(1),
                        returnDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        status = reader.GetString(3),
                        movies = new List<RentalMoviesResponseDto>()
                    };
                    rentalsById.Add(rentalId, rental);
                }

                if (!reader.IsDBNull(4))
                {
                    rental.movies.Add(new RentalMoviesResponseDto
                    {
                        title = reader.GetString(4),
                        priceAtRental = reader.GetDecimal(5)
                    });
                }
            }
        }

        var response = new CustomerRentalsResponseDto
        {
            firstName = firstName,
            lastName = lastName,
            rentals = rentalsById.Values.ToList(),
        };

        if (response is null) return NotFound();
        return Ok(response);

    }

    [HttpPost("{id}/rentals")]
    public async Task<IActionResult> CreateRentalAsync(int id, CreateRentalRequestDto dto)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            await using (var customerCheck = new SqlCommand(
                             "SELECT 1 FROM Customer WHERE customer_id = @customerId;", connection, transaction))
            {
                customerCheck.Parameters.AddWithValue("@customerId", id);
                var exists = await customerCheck.ExecuteScalarAsync();
                if (exists == null) return NotFound();
            }

            var movieIds = new List<int>();
            foreach (var movie in dto.movies)
            {
                await using var movieCommand = new SqlCommand("""
                                                              SELECT movie_id FROM Movie WHERE title = @title;
                                                              """, connection,transaction);
                movieCommand.Parameters.AddWithValue("@title", movie.title);
                
                var movieId = await movieCommand.ExecuteScalarAsync();
                if (movieId is null) return NotFound();
                
                movieIds.Add((int)movieId);
            }

            int statusId;
            await using (var statusCommand = new SqlCommand("SELECT status_id FROM Status WHERE name = @name;",
                             connection, transaction))
            {
                statusCommand.Parameters.AddWithValue("@name", "Rented");
                var statusResult = await statusCommand.ExecuteScalarAsync();
                if (statusResult is null) return BadRequest();
                statusId = (int)statusResult;
            }

            await using (var rentalCommand = new SqlCommand("""
                                                            SET IDENTITY_INSERT Rental ON;
                                                            INSERT INTO Rental (rental_id, rental_date, return_date, customer_id, status_id)
                                                            VALUES (@rentalId,  @rentalDate, NULL, @customerId, @statusId);
                                                            SET IDENTITY_INSERT Rental OFF;
                                                            """, connection, transaction))
            {
                rentalCommand.Parameters.AddWithValue("@rentalId", dto.id);
                rentalCommand.Parameters.AddWithValue("@rentalDate", dto.rentalDate);
                rentalCommand.Parameters.AddWithValue("@customerId", id);
                rentalCommand.Parameters.AddWithValue("@statusId", statusId);
                
                await rentalCommand.ExecuteNonQueryAsync();
            }

            for (var i = 0; i < dto.movies.Count; i++)
            {
                await using var itemCommand = new SqlCommand("""
                                                             INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental)
                                                             VALUES (@rentalId, @movieId, @priceAtRental);
                                                             """, connection, transaction);
                itemCommand.Parameters.AddWithValue("@rentalId", dto.id);
                itemCommand.Parameters.AddWithValue("@movieId", movieIds[i]);
                itemCommand.Parameters.AddWithValue("@priceAtRental", dto.movies[i].rentalPrice);
                
                await itemCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok();
    }
}