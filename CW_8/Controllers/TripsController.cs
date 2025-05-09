using Microsoft.AspNetCore.Mvc;

namespace CW_8.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    // 1. GET /api/trips - Pobieranie wszystkich wycieczek
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var query = "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS Country " +
                    "FROM Trip t " +
                    "JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip " +
                    "JOIN Country c ON ct.IdCountry = c.IdCountry";

        var trips = await DatabaseHelper.ExecuteQueryAsync(query, reader => new Trip
        {
            IdTrip = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            DateFrom = reader.GetDateTime(3),
            DateTo = reader.GetDateTime(4),
            MaxPeople = reader.GetInt32(5)
        });

        return Ok(trips);
    }

    // 2. GET /api/clients/{id}/trips - Wycieczki klienta
    [HttpGet("clients/{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var query =
            "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate " +
            "FROM Client_Trip ct " +
            "JOIN Trip t ON ct.IdTrip = t.IdTrip " +
            "WHERE ct.IdClient = @Id";

        var clientTrips = await DatabaseHelper.ExecuteQueryAsync(query, reader => new Trip
        {
            IdTrip = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            DateFrom = reader.GetDateTime(3),
            DateTo = reader.GetDateTime(4),
            MaxPeople = reader.GetInt32(5)
        });

        if (clientTrips.Count == 0)
        {
            return NotFound();
        }

        return Ok(clientTrips);
    }

    // 3. POST /api/clients - Dodawanie klienta
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] Client client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) || string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email))
        {
            return BadRequest("Wszystkie dane są wymagane.");
        }

        var query = "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) " +
                    "VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); SELECT SCOPE_IDENTITY();";

        var parameters = new[]
        {
            new SqlParameter("@FirstName", client.FirstName),
            new SqlParameter("@LastName", client.LastName),
            new SqlParameter("@Email", client.Email),
            new SqlParameter("@Telephone", client.Telephone ?? (object)DBNull.Value),
            new SqlParameter("@Pesel", client.Pesel ?? (object)DBNull.Value)
        };

        var clientId = await DatabaseHelper.ExecuteNonQueryAsync(query, parameters);
        return CreatedAtAction(nameof(GetClientTrips), new { id = clientId }, clientId);
    }

    // 4. PUT /api/clients/{id}/trips/{tripId} - Rejestracja klienta na wycieczkę
    [HttpPut("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
    {
        // Check if client exists (for simplicity, assume it exists)
        var clientQuery = "SELECT 1 FROM Client WHERE IdClient = @Id";
        var clientExists = await DatabaseHelper.ExecuteQueryAsync(clientQuery, reader => reader.GetInt32(0));

        if (clientExists.Count == 0)
        {
            return NotFound("Klient nie istnieje.");
        }

        // Check if trip exists
        var tripQuery = "SELECT 1 FROM Trip WHERE IdTrip = @Id";
        var tripExists = await DatabaseHelper.ExecuteQueryAsync(tripQuery, reader => reader.GetInt32(0));

        if (tripExists.Count == 0)
        {
            return NotFound("Wycieczka nie istnieje.");
        }

        var maxPeopleQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
        var maxPeople = await DatabaseHelper.ExecuteQueryAsync(maxPeopleQuery, reader => reader.GetInt32(0));

        // Check if maximum capacity reached
        var currentParticipantsQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId";
        var currentParticipants =
            await DatabaseHelper.ExecuteQueryAsync(currentParticipantsQuery, reader => reader.GetInt32(0));

        if (currentParticipants.Count >= maxPeople[0])
        {
            return BadRequest("Wycieczka jest już pełna.");
        }

        var insert Query =
            "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@IdClient, @IdTrip, @RegisteredAt)";
        var insertParams = new[]
            {
                new SqlParameter("@IdClient", id),
                new SqlParameter("@IdTrip", tripId),
                new SqlParameter("@RegisteredAt", DateTime.Now)

                await DatabaseHelper.ExecuteNonQueryAsync(insertQuery, insertParams);
                return Ok("Klient zarejestrowany.");
            }

            // 5. DELETE /api/clients/{id}/trips/{tripId} - Usuwanie rejestracji
            [HttpDelete("clients/{id}/trips/{tripId}")]

        public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
        {
            var existsQuery = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            var exists = await DatabaseHelper.ExecuteQueryAsync(existsQuery, reader => reader.GetInt32(0));

            if (exists.Count == 0)
            {
                return NotFound("Rejestracja nie istnieje.");
            }

            var deleteQuery = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            var deleteParams = new[]
            {
                new SqlParameter("@IdClient", id),
                new SqlParameter("@IdTrip", tripId)
            };

            await DatabaseHelper.ExecuteNonQueryAsync(deleteQuery, deleteParams);
            return Ok("Rejestracja usunięta.");
        }
    }
}