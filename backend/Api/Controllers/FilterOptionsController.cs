using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilterOptionsController : ControllerBase
{
    private readonly OracleConnection _connection;

    public FilterOptionsController(OracleConnection connection)
    {
        _connection = connection;
    }

    [HttpGet]
    public async Task<IActionResult> GetFilterOptionsData([FromQuery] string region = null)
    {
        try
        {
            await _connection.OpenAsync();

            var regions = new List<string>();
            var countries = new List<string>();
            var channels = new List<string>();
            var years = new List<int>();
            var categories = new List<string>();

            using (var cmd = new OracleCommand("SELECT DISTINCT country_region FROM countries ORDER BY country_region", _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    regions.Add(reader.GetString(0));
            }

            string countriesQuery = "SELECT DISTINCT country_name FROM countries";
            if (!string.IsNullOrEmpty(region))
                countriesQuery += " WHERE country_region = :region";
            countriesQuery += " ORDER BY country_name";

            using (var cmd = new OracleCommand(countriesQuery, _connection))
            {
                if (!string.IsNullOrEmpty(region))
                    cmd.Parameters.Add(new OracleParameter("region", region));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    countries.Add(reader.GetString(0));
            }

            using (var cmd = new OracleCommand("SELECT DISTINCT channel_desc FROM channels WHERE channel_desc != 'Catalog' ORDER BY channel_desc", _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    channels.Add(reader.GetString(0));
            }

            using (var cmd = new OracleCommand("SELECT DISTINCT calendar_year FROM times ORDER BY calendar_year", _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    years.Add(reader.GetInt32(0));
            }

            using (var cmd = new OracleCommand("SELECT DISTINCT prod_category FROM products ORDER BY prod_category", _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    categories.Add(reader.GetString(0));
            }

            return Ok(new
            {
                Regions = regions,
                Countries = countries,
                Channels = channels,
                Years = years,
                Categories = categories
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }
}
