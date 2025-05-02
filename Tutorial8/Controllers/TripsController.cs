using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        [HttpGet("/api/clients/{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            try
            {
                var trips = await _tripsService.GetTripsForClient(id);
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
        // POST /api/clients
        [HttpPost("/api/clients")]
        public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO dto)
        {
            try
            {
                var newId = await _tripsService.CreateClient(dto);
                return CreatedAtAction(null, new { id = newId }, $"Utworzono klienta o ID {newId}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    
    // PUT /api/clients/{id}/trips/{tripId}
    [HttpPut("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
    {
        try
        {
            // Sprawdzamy, czy klient i wycieczka istnieją
            bool clientExists = await _tripsService.ClientExists(id);
            if (!clientExists)
            {
                return NotFound(new { message = "Klient o podanym ID nie istnieje." });
            }

            bool tripExists = await _tripsService.TripExists(tripId);
            if (!tripExists)
            {
                return NotFound(new { message = "Wycieczka o podanym ID nie istnieje." });
            }

            // Rejestrujemy klienta na wycieczce
            bool registrationSuccessful = await _tripsService.RegisterClientForTrip(id, tripId);
            if (registrationSuccessful)
            {
                return Ok(new { message = "Klient pomyślnie zarejestrowany na wycieczkę." });
            }
            else
            {
                return BadRequest(new { message = "Klient jest już zarejestrowany na tę wycieczkę." });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        
    }
    // DELETE /api/clients/{id}/trips/{tripId}
    [HttpDelete("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        var success = await _tripsService.UnregisterClientFromTrip(id, tripId);
    
        if (success)
        {
            return Ok(new { message = "Klient został wypisany z wycieczki." });
        }

        return NotFound(new { message = "Rejestracja klienta na tej wycieczce nie istnieje." });
    }

    }
    
 
}
