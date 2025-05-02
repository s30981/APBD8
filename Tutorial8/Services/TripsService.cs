using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
        {
            var trips = new List<TripDTO>();

            string command = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, 
                       c.IdCountry, c.Name AS CountryName
                FROM Trip t
                LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var tripDict = new Dictionary<int, TripDTO>();

                    while (await reader.ReadAsync())
                    {
                        int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                        if (!tripDict.ContainsKey(tripId))
                        {
                            var trip = new TripDTO
                            {
                                Id = tripId,
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                                Countries = new List<CountryDTO>()
                            };
                            tripDict[tripId] = trip;
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("IdCountry")))
                        {
                            var country = new CountryDTO
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("IdCountry")),
                                Name = reader.GetString(reader.GetOrdinal("CountryName"))
                            };

                            tripDict[tripId].Countries.Add(country);
                        }
                    }

                    trips.AddRange(tripDict.Values);
                }
            }

            return trips;
        }
    
    
    public async Task<List<TripDTO>> GetTripsForClient(int clientId)
{
    var trips = new List<TripDTO>();
    var tripDict = new Dictionary<int, TripDTO>();

    string query = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               c.Name AS CountryName
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
        LEFT JOIN Country c ON ctr.IdCountry = c.IdCountry
        WHERE ct.IdClient = @clientId
        ORDER BY t.IdTrip";

    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @clientId", conn);
    checkCmd.Parameters.AddWithValue("@clientId", clientId);
    var exists = (int)await checkCmd.ExecuteScalarAsync();
    if (exists == 0)
        throw new Exception("Client not found");

    using var cmd = new SqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@clientId", clientId);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));

        if (!tripDict.ContainsKey(idTrip))
        {
            var trip = new TripDTO
            {
                Id = idTrip,
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                Countries = new List<CountryDTO>()
            };
            tripDict[idTrip] = trip;
            trips.Add(trip);
        }

        if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
        {
            tripDict[idTrip].Countries.Add(new CountryDTO
            {
                Name = reader.GetString(reader.GetOrdinal("CountryName"))
            });
        }
    }

    return trips;
}

    public async Task<int> CreateClient(ClientCreateDTO client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Pesel))
        {
            throw new ArgumentException("FirstName, LastName i Pesel są wymagane.");
        }

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@fn, @ln, @email, @tel, @pesel)", conn);

        cmd.Parameters.AddWithValue("@fn", client.FirstName);
        cmd.Parameters.AddWithValue("@ln", client.LastName);
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(client.Email) ? DBNull.Value : client.Email);
        cmd.Parameters.AddWithValue("@tel", string.IsNullOrWhiteSpace(client.Telephone) ? DBNull.Value : client.Telephone);
        cmd.Parameters.AddWithValue("@pesel", client.Pesel);

        var insertedId = (int)await cmd.ExecuteScalarAsync();
        return insertedId;
    }
    
       public async Task<bool> ClientExists(int clientId)
        {
            string command = "SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return (int)result > 0;
            }
        }

        public async Task<bool> TripExists(int tripId)
        {
            string command = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @TripId";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return (int)result > 0;
            }
        }

        public async Task<bool> RegisterClientForTrip(int clientId, int tripId)
        {
            int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

            string command = @"
        IF NOT EXISTS (SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId)
        BEGIN
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@ClientId, @TripId, @RegisteredAt)
            SELECT 1
        END
        ELSE
        BEGIN
            SELECT 0
        END";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@RegisteredAt", registeredAt);

                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return (int)result == 1;
            }
        }
        public async Task<bool> UnregisterClientFromTrip(int clientId, int tripId)
        {
            string command = @"
        DELETE FROM Client_Trip 
        WHERE IdClient = @ClientId AND IdTrip = @TripId";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@TripId", tripId);

                await conn.OpenAsync();
                int affectedRows = await cmd.ExecuteNonQueryAsync();
                return affectedRows > 0;
            }
        }

    
}