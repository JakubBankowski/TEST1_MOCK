using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TEST1_MOCK.DTOs;
using TEST1_MOCK.Exceptions;
using TEST1_MOCK.Repositories;

namespace TEST1_MOCK.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IRentalRepository _service;
    
    public CustomersController(IRentalRepository service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        try
        {
            var customer = await _service.GetCustomerAsync(id);
            return Ok(customer);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{id}/rentals")]
    public async Task<IActionResult> GetCustomerRentals(int id)
    {
        var response = await _service.GetCustomerRentalsAsync(id);
        return Ok(response);

    }

    [HttpPost("{id}/rentals")]
    public async Task<IActionResult> CreateRental(int id, CreateRentalRequestDto dto)
    {
        try
        {
            await _service.CreateRentalAsync(id, dto);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (BadRequestException e)
        {
            return BadRequest(e.Message);
        }
        
        return CreatedAtAction(nameof(GetCustomerRentals), new { id }, null);
    }
}