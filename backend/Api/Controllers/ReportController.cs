using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly OracleConnection _connection;

    public ReportController(OracleConnection connection)
    {
        _connection = connection;
    }

    [HttpGet("top-data")]
public async Task<IActionResult> GetTopData(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        var sql = @"
            SELECT
                SUM(s.amount_sold) AS total_sales,
                COUNT(DISTINCT c.cust_id) AS customers,
                SUM(s.quantity_sold * p.prod_min_price) AS total_cost,
                SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit,
                CASE 
                    WHEN SUM(s.amount_sold) = 0 THEN 0
                    ELSE ROUND((SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price)) / SUM(s.amount_sold) * 100, 2)
                END AS profit_percent
            FROM sales s
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        if (!string.IsNullOrEmpty(whereClause))
            sql += whereClause;

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new
            {
                TotalSales = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0),
                Customers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                TotalCost = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                Profit = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                ProfitPercent = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
            };

            return Ok(result);
        }

        return Ok(new { TotalSales = 0, Customers = 0, TotalCost = 0, Profit = 0, ProfitPercent = 0 });
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



    [HttpGet("yearly-sales")]
public async Task<IActionResult> GetYearlySales(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        var sql = @"
            SELECT
                t.calendar_year AS year,
                SUM(s.quantity_sold) AS total_quantity
            FROM sales s
            JOIN times t ON s.time_id = t.time_id
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + " GROUP BY t.calendar_year ORDER BY t.calendar_year";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();
        var yearlySales = new List<object>();

        while (await reader.ReadAsync())
        {
            yearlySales.Add(new
            {
                Year = reader.GetString(0),
                TotalQuantity = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
            });
        }

        return Ok(yearlySales);
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

[HttpGet("monthly-sales-with-previous")]
public async Task<IActionResult> GetMonthlySalesWithPrevious(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        string sql;
        var parameters = new List<OracleParameter>();

        if (year.HasValue)
        {
            int prevYear = year.Value - 1;

            sql = @"
                SELECT
                    t.calendar_month_name AS month,
                    SUM(CASE WHEN t.calendar_year = :year THEN s.quantity_sold ELSE 0 END) AS current_year,
                    SUM(CASE WHEN t.calendar_year = :prevYear THEN s.quantity_sold ELSE 0 END) AS previous_year
                FROM sales s
                JOIN times t ON s.time_id = t.time_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
                JOIN products p ON s.prod_id = p.prod_id
                WHERE t.calendar_year IN (:year, :prevYear)
            ";

            parameters.Add(new OracleParameter("year", year.Value));
            parameters.Add(new OracleParameter("prevYear", prevYear));

            // Filtry pozostałe niż rok
            var (whereClause, filterParams) = BuildFilters(region, country, channel, null, monthFrom, monthTo, category);
            if (!string.IsNullOrEmpty(whereClause))
                sql += " AND " + whereClause.Replace("WHERE", "");

            sql += " GROUP BY t.calendar_month_name, t.calendar_month_number ORDER BY t.calendar_month_number";
            parameters.AddRange(filterParams);
        }
        else
        {
            sql = @"
                SELECT
                    t.calendar_year || '-' || t.calendar_month_name AS month,
                    SUM(s.quantity_sold) AS total_sales
                FROM sales s
                JOIN times t ON s.time_id = t.time_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
                JOIN products p ON s.prod_id = p.prod_id
            ";

            var (whereClause, filterParams) = BuildFilters(region, country, channel, null, monthFrom, monthTo, category);
            if (!string.IsNullOrEmpty(whereClause))
                sql += " " + whereClause; 

            sql += " GROUP BY t.calendar_year, t.calendar_month_number, t.calendar_month_name ORDER BY t.calendar_year, t.calendar_month_number";
            parameters.AddRange(filterParams);
        }

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();
        var monthlySales = new List<object>();

        while (await reader.ReadAsync())
        {
            if (year.HasValue)
            {
                monthlySales.Add(new
                {
                    Month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    CurrentYear = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    PreviousYear = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                });
            }
            else
            {
                monthlySales.Add(new
                {
                    Month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    TotalSales = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    CurrentYear = 0,
                    PreviousYear = 0
                });
            }
        }

        return Ok(monthlySales);
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









    [HttpGet("top-products")]
public async Task<IActionResult> GetTopProducts(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        decimal totalSales = 0;
        using (var totalCmd = new OracleCommand(@"
            SELECT SUM(s.amount_sold)
            FROM sales s
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        " + BuildFilters(region, country, channel, year, monthFrom, monthTo, category).whereClause, _connection))
        {
            var (_, totalParams) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
            totalCmd.Parameters.AddRange(totalParams.ToArray());
            var totalResult = await totalCmd.ExecuteScalarAsync();
            totalSales = totalResult == DBNull.Value ? 0 : Convert.ToDecimal(totalResult);
        }

        var sql = @"
            SELECT
                p.prod_name AS product,
                SUM(s.quantity_sold) AS total_quantity,
                SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit
            FROM sales s
            JOIN products p ON s.prod_id = p.prod_id
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + " GROUP BY p.prod_name ORDER BY SUM(s.quantity_sold) DESC FETCH FIRST 4 ROWS ONLY";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();
        var topProducts = new List<object>();

        while (await reader.ReadAsync())
        {
            var profit = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
            var profitPercent = totalSales == 0 ? 0 : Math.Round((profit / totalSales) * 100, 2);

            topProducts.Add(new
            {
                Product = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                TotalQuantity = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Profit = profit,
                ProfitPercent = profitPercent
            });
        }

        return Ok(topProducts);
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



[HttpGet("sales-vs-promotions")]
public async Task<IActionResult> GetSalesVsPromotions(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        string sql;
        if (year.HasValue)
        {
            sql = @"
                SELECT
                    t.calendar_month_name AS month,
                    p.promo_category,
                    SUM(s.quantity_sold) AS category_sales,
                    SUM(SUM(s.quantity_sold)) OVER (PARTITION BY t.calendar_year, t.calendar_month_name) AS total_sales
                FROM sales s
                JOIN promotions p ON s.promo_id = p.promo_id
                JOIN times t ON s.time_id = t.time_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
                JOIN products pr ON s.prod_id = pr.prod_id
                WHERE t.calendar_year = :year
            ";
        }
        else
        {
            sql = @"
                SELECT
                    t.calendar_year AS year,
                    p.promo_category,
                    SUM(s.quantity_sold) AS category_sales,
                    SUM(SUM(s.quantity_sold)) OVER (PARTITION BY t.calendar_year) AS total_sales
                FROM sales s
                JOIN promotions p ON s.promo_id = p.promo_id
                JOIN times t ON s.time_id = t.time_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
                JOIN products pr ON s.prod_id = pr.prod_id
            ";
        }

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);

        if (!string.IsNullOrEmpty(whereClause))
        {
            if (year.HasValue)
            {
                sql += " AND " + whereClause.Replace("WHERE", "");
            }
            else
            {
                sql += " WHERE " + whereClause.Replace("WHERE", "");
            }
        }

        sql += year.HasValue
            ? " GROUP BY t.calendar_year, t.calendar_month_name, t.calendar_month_desc, p.promo_category ORDER BY t.calendar_year, MIN(t.calendar_month_desc)"
            : " GROUP BY t.calendar_year, p.promo_category ORDER BY t.calendar_year";

        using var cmd = new OracleCommand(sql, _connection);
        if (year.HasValue)
            cmd.Parameters.Add(new OracleParameter("year", year.Value));
        cmd.Parameters.AddRange(parameters.ToArray());

        var rawData = new List<(string Key, string Category, decimal CategorySales, decimal TotalSales)>();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var key = year.HasValue
                ? reader.IsDBNull(0) ? "Unknown" : reader.GetString(0)
                : reader.IsDBNull(0) ? "Unknown" : reader.GetInt32(0).ToString();
            var categoryName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
            var categorySales = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2));
            var totalSales = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3));

            rawData.Add((key, categoryName, categorySales, totalSales));
        }

        var grouped = rawData
            .GroupBy(r => new { r.Key, r.TotalSales })
            .Select(g =>
            {
                var dict = new Dictionary<string, object>
                {
                    [year.HasValue ? "month" : "year"] = g.Key.Key,
                    ["totalSales"] = g.Key.TotalSales
                };

                foreach (var row in g)
                {
                    var percent = g.Key.TotalSales == 0 ? 0 : Math.Round((row.CategorySales / g.Key.TotalSales) * 100, 2);
                    dict[row.Category] = percent;
                }

                return dict;
            })
            .ToList();

        return Ok(grouped);
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





    [HttpGet("top-products-profit")]
public async Task<IActionResult> GetTopProductsProfit(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        decimal totalSales = 0;
        var totalSql = @"
            SELECT SUM(s.amount_sold)
            FROM sales s
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (totalWhere, totalParams) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        totalSql += totalWhere;

        using (var totalCmd = new OracleCommand(totalSql, _connection))
        {
            totalCmd.Parameters.AddRange(totalParams.ToArray());
            var totalResult = await totalCmd.ExecuteScalarAsync();
            totalSales = totalResult == DBNull.Value ? 0 : Convert.ToDecimal(totalResult);
        }

        var sql = @"
            SELECT
                p.prod_name AS product,
                SUM(s.amount_sold) AS total_sales,
                SUM(s.quantity_sold) AS total_quantity,
                SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit
            FROM sales s
            JOIN products p ON s.prod_id = p.prod_id
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + " GROUP BY p.prod_name ORDER BY SUM(s.amount_sold) DESC FETCH FIRST 4 ROWS ONLY";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        var result = new List<object>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var total = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            var totalQuantity = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
            var profit = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
            var percentOfTotalSales = totalSales == 0 ? 0 : Math.Round((total / totalSales) * 100, 2);
            var profitPercent = total == 0 ? 0 : Math.Round((profit / total) * 100, 2);

            result.Add(new
            {
                Product = reader.GetString(0),
                TotalSales = total,
                TotalQuantity = totalQuantity,
                Profit = profit,
                PercentOfTotalSales = percentOfTotalSales,
                ProfitPercent = profitPercent
            });
        }

        return Ok(result);
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




    [HttpGet("sales-by-region-country")]
public async Task<IActionResult> GetSalesByRegionCountry(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        var sql = @"
            SELECT
                co.country_region AS region,
                co.country_name AS country,
                SUM(s.quantity_sold) AS total_quantity
            FROM sales s
            JOIN customers cu ON s.cust_id = cu.cust_id
            JOIN countries co ON cu.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + " GROUP BY co.country_region, co.country_name ORDER BY co.country_region, co.country_name";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();

        var tempData = new Dictionary<string, Dictionary<string, int>>();

        while (await reader.ReadAsync())
        {
            string reg = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
            string ctry = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
            int quantity = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            if (!tempData.ContainsKey(reg))
                tempData[reg] = new Dictionary<string, int>();

            tempData[reg][ctry] = quantity;
        }

        var result = tempData.Select(r =>
        {
            var obj = new Dictionary<string, object> { ["region"] = r.Key };
            foreach (var kvp in r.Value)
            {
                obj[kvp.Key] = kvp.Value;
            }
            return obj;
        }).ToList();

        return Ok(result);
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


[HttpGet("sales-costs")]
public async Task<IActionResult> GetSalesAndCosts(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        if (year.HasValue)
        {
            var sql = @"
                SELECT
                    t.calendar_month_name AS month,
                    SUM(s.amount_sold) AS total_sales,
                    SUM(s.quantity_sold * p.prod_min_price) AS total_cost
                FROM sales s
                JOIN times t ON s.time_id = t.time_id
                JOIN products p ON s.prod_id = p.prod_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
                WHERE t.calendar_year = :year
            ";

            var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
            if (!string.IsNullOrEmpty(whereClause))
                sql += " AND " + whereClause.Replace("WHERE", "");

            sql += " GROUP BY t.calendar_month_name, t.calendar_month_number ORDER BY t.calendar_month_number";

            using var cmd = new OracleCommand(sql, _connection);
            cmd.Parameters.Add(new OracleParameter("year", year));
            cmd.Parameters.AddRange(parameters.ToArray());

            var monthlyData = new List<object>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                monthlyData.Add(new
                {
                    Month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    Amount_Sold = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                    Cost = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2)
                });
            }

            return Ok(monthlyData);
        }
        else
        {
            var sql = @"
                SELECT
                    t.calendar_year AS year,
                    SUM(s.amount_sold) AS total_sales,
                    SUM(s.quantity_sold * p.prod_min_price) AS total_cost
                FROM sales s
                JOIN times t ON s.time_id = t.time_id
                JOIN products p ON s.prod_id = p.prod_id
                JOIN customers c ON s.cust_id = c.cust_id
                JOIN countries co ON c.country_id = co.country_id
                JOIN channels ch ON s.channel_id = ch.channel_id
            ";

            var (whereClause, parameters) = BuildFilters(region, country, channel, null, monthFrom, monthTo, category);
            if (!string.IsNullOrEmpty(whereClause))
                sql += " " + whereClause;

            sql += " GROUP BY t.calendar_year ORDER BY t.calendar_year";

            using var cmd = new OracleCommand(sql, _connection);
            cmd.Parameters.AddRange(parameters.ToArray());

            var yearlyData = new List<object>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                yearlyData.Add(new
                {
                    Year = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    TotalSales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                    TotalCost = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2)
                });
            }

            return Ok(yearlyData);
        }
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


[HttpGet("top-customers")]
public async Task<IActionResult> GetTopCustomers(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        int topCount = 5;

        decimal totalSales = 0;
        var totalSql = @"
            SELECT SUM(s.amount_sold)
            FROM sales s
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (totalWhere, totalParams) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        totalSql += totalWhere;

        using (var totalCmd = new OracleCommand(totalSql, _connection))
        {
            totalCmd.Parameters.AddRange(totalParams.ToArray());
            var totalResult = await totalCmd.ExecuteScalarAsync();
            totalSales = totalResult == DBNull.Value ? 0 : Convert.ToDecimal(totalResult);
        }

        var sql = @"
            SELECT 
                c.cust_email,
                SUM(s.amount_sold) AS sales
            FROM sales s
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + $" GROUP BY c.cust_email ORDER BY SUM(s.amount_sold) DESC FETCH FIRST {topCount} ROWS ONLY";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        var result = new List<object>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var custEmail = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
            var sales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            var percentOfTotal = totalSales == 0 ? 0 : Math.Round((sales / totalSales) * 100, 2);

            result.Add(new
            {
                cust_email = custEmail,
                sales = sales,
                percentOfTotal = percentOfTotal
            });
        }

        return Ok(result);
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



[HttpGet("sales-by-channel")]
public async Task<IActionResult> GetSalesByChannel(
    [FromQuery] string region = null,
    [FromQuery] string country = null,
    [FromQuery] string channel = null,
    [FromQuery] int? year = null,
    [FromQuery] int? monthFrom = null,
    [FromQuery] int? monthTo = null,
    [FromQuery] string category = null)
{
    try
    {
        await _connection.OpenAsync();

        var sql = @"
            SELECT 
                ch.channel_desc,
                SUM(s.amount_sold) AS total_sales
            FROM sales s
            JOIN channels ch ON s.channel_id = ch.channel_id
            JOIN customers c ON s.cust_id = c.cust_id
            JOIN countries co ON c.country_id = co.country_id
            JOIN products p ON s.prod_id = p.prod_id
            JOIN times t ON s.time_id = t.time_id
        ";

        var (whereClause, parameters) = BuildFilters(region, country, channel, year, monthFrom, monthTo, category);
        sql += whereClause + " GROUP BY ch.channel_desc ORDER BY total_sales DESC";

        using var cmd = new OracleCommand(sql, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        var rawData = new List<(string Channel, decimal TotalSales)>();
        decimal totalAllChannels = 0;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var ch = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
            var sales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            totalAllChannels += sales;
            rawData.Add((ch, sales));
        }

        var result = rawData.Select(r => new
        {
            channel = r.Channel,
            totalQuantity = r.TotalSales,
            percentOfTotal = totalAllChannels == 0 ? 0 : Math.Round((r.TotalSales / totalAllChannels) * 100, 2)
        }).ToList();

        return Ok(result);
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





    private (string whereClause, List<OracleParameter> parameters) BuildFilters(
        string region, string country, string channel,
        int? year, int? monthFrom, int? monthTo, string category)
    {
        var whereClauses = new List<string>();
        var parameters = new List<OracleParameter>();

        if (!string.IsNullOrEmpty(region))
        {
            whereClauses.Add("co.country_region = :region");
            parameters.Add(new OracleParameter("region", region));
        }
        if (!string.IsNullOrEmpty(country))
        {
            whereClauses.Add("co.country_name = :country");
            parameters.Add(new OracleParameter("country", country));
        }
        if (!string.IsNullOrEmpty(channel))
        {
            whereClauses.Add("ch.channel_desc = :channel");
            parameters.Add(new OracleParameter("channel", channel));
        }
        if (year.HasValue)
        {
            whereClauses.Add("t.calendar_year = :year");
            parameters.Add(new OracleParameter("year", year.Value));
        }
        if (monthFrom.HasValue)
        {
            whereClauses.Add("t.calendar_month_number >= :monthFrom");
            parameters.Add(new OracleParameter("monthFrom", monthFrom.Value));
        }
        if (monthTo.HasValue)
        {
            whereClauses.Add("t.calendar_month_number <= :monthTo");
            parameters.Add(new OracleParameter("monthTo", monthTo.Value));
        }
        if (!string.IsNullOrEmpty(category))
        {
            whereClauses.Add("p.prod_category = :category");
            parameters.Add(new OracleParameter("category", category));
        }

        var whereClause = whereClauses.Any()
            ? " WHERE " + string.Join(" AND ", whereClauses)
            : "";

        return (whereClause, parameters);
    }

}











// using Microsoft.AspNetCore.Mvc;
// using Oracle.ManagedDataAccess.Client;

// namespace Api.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class ReportController : ControllerBase
// {
//     private readonly OracleConnection _connection;

//     public ReportController(OracleConnection connection)
//     {
//         _connection = connection;
//     }

//     [HttpGet("top-data")]
//     public async Task<IActionResult> GetTopData(
//         [FromQuery] string region = null, 
//         [FromQuery] string country = null, 
//         [FromQuery] string channel = null,
//         
//         [FromQuery] int? year = null,
//         [FromQuery] int? monthFrom = null,
//         [FromQuery] int? monthTo = null,
//         [FromQuery] string category = null)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             var sql = @"
//                 SELECT
//                     SUM(s.amount_sold) AS total_sales,
//                     COUNT(DISTINCT c.cust_id) AS customers,
//                     SUM(s.quantity_sold * p.prod_min_price) AS total_cost,
//                     SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit,
//                     CASE 
//                         WHEN SUM(s.amount_sold) = 0 THEN 0
//                         ELSE ROUND((SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price)) / SUM(s.amount_sold) * 100, 2)
//                     END AS profit_percent
//                 FROM sales s
//                 JOIN customers c ON s.cust_id = c.cust_id
//                 JOIN products p ON s.prod_id = p.prod_id
//                 JOIN countries co ON c.country_id = co.country_id
//                 JOIN channels ch ON s.channel_id = ch.channel_id
//                 JOIN times t ON s.time_id = t.time_id
//             ";

//             var whereClauses = new List<string>();

//             if (!string.IsNullOrEmpty(region))
//                 whereClauses.Add("co.country_region = :region");
//             if (!string.IsNullOrEmpty(country))
//                 whereClauses.Add("co.country_name = :country");
//             if (!string.IsNullOrEmpty(channel))
//                 whereClauses.Add("ch.channel_desc = :channel");
//             if (yearFrom.HasValue)
//                 whereClauses.Add("t.calendar_year >= :yearFrom");
//             if (yearTo.HasValue)
//                 whereClauses.Add("t.calendar_year <= :yearTo");
//             if (monthFrom.HasValue)
//                 whereClauses.Add("t.calendar_month_number >= :monthFrom");
//             if (monthTo.HasValue)
//                 whereClauses.Add("t.calendar_month_number <= :monthTo");
//             if (!string.IsNullOrEmpty(category))
//                 whereClauses.Add("p.prod_category = :category");

//             if (whereClauses.Any())
//                 sql += " WHERE " + string.Join(" AND ", whereClauses);

//             using var cmd = new OracleCommand(sql, _connection);

//             if (!string.IsNullOrEmpty(region))
//                 cmd.Parameters.Add(new OracleParameter("region", region));
//             if (!string.IsNullOrEmpty(country))
//                 cmd.Parameters.Add(new OracleParameter("country", country));
//             if (!string.IsNullOrEmpty(channel))
//                 cmd.Parameters.Add(new OracleParameter("channel", channel));
//             if (yearFrom.HasValue)
//                 cmd.Parameters.Add(new OracleParameter("yearFrom", yearFrom.Value));
//             if (yearTo.HasValue)
//                 cmd.Parameters.Add(new OracleParameter("yearTo", yearTo.Value));
//             if (monthFrom.HasValue)
//                 cmd.Parameters.Add(new OracleParameter("monthFrom", monthFrom.Value));
//             if (monthTo.HasValue)
//                 cmd.Parameters.Add(new OracleParameter("monthTo", monthTo.Value));
//             if (!string.IsNullOrEmpty(category))
//                 cmd.Parameters.Add(new OracleParameter("category", category));

//             using var reader = await cmd.ExecuteReaderAsync();

//             if (await reader.ReadAsync())
//             {
//                 var result = new
//                 {
//                     TotalSales = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0),
//                     Customers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
//                     TotalCost = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
//                     Profit = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
//                     ProfitPercent = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
//                 };

//                 return Ok(result);
//             }

//             return Ok(new { TotalSales = 0, Customers = 0, TotalCost = 0, Profit = 0, ProfitPercent = 0 });
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }


//     [HttpGet("yearly-sales")]
//     public async Task<IActionResult> GetYearlySales()
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     t.calendar_year AS year,
//                     SUM(s.quantity_sold) AS total_quantity
//                 FROM sales s
//                 JOIN times t ON s.time_id = t.time_id
//                 GROUP BY t.calendar_year
//                 ORDER BY t.calendar_year
//             ", _connection);

//             using var reader = await cmd.ExecuteReaderAsync();
//             var yearlySales = new List<object>();

//             while (await reader.ReadAsync())
//             {
//                 yearlySales.Add(new
//                 {
//                     Year = reader.GetString(0),
//                     TotalQuantity = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
//                 });
//             }

//             return Ok(yearlySales);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }

//     [HttpGet("monthly-sales-with-previous/{year}")]
//     public async Task<IActionResult> GetMonthlySalesWithPrevious(int year)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     t.calendar_month_name AS month,
//                     SUM(CASE WHEN t.calendar_year = :year THEN s.quantity_sold ELSE 0 END) AS current_year,
//                     SUM(CASE WHEN t.calendar_year = :prevYear THEN s.quantity_sold ELSE 0 END) AS previous_year
//                 FROM sales s
//                 JOIN times t ON s.time_id = t.time_id
//                 WHERE t.calendar_year IN (:year, :prevYear)
//                 GROUP BY t.calendar_month_name, t.calendar_month_number
//                 ORDER BY t.calendar_month_number
//             ", _connection);

//             int prevYear = year - 1;
//             cmd.Parameters.Add(new OracleParameter("year", year));
//             cmd.Parameters.Add(new OracleParameter("prevYear", prevYear));

//             using var reader = await cmd.ExecuteReaderAsync();
//             var monthlySales = new List<object>();

//             while (await reader.ReadAsync())
//             {
//                 monthlySales.Add(new
//                 {
//                     Month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
//                     CurrentYear = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
//                     PreviousYear = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
//                 });
//             }

//             return Ok(monthlySales);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }



//    [HttpGet("top-products/{topCount?}")]
//     public async Task<IActionResult> GetTopProducts(int topCount = 4)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             decimal totalSales = 0;
//             using (var totalCmd = new OracleCommand("SELECT SUM(amount_sold) FROM sales", _connection))
//             {
//                 totalSales = Convert.ToDecimal(await totalCmd.ExecuteScalarAsync());
//             }

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     p.prod_name AS product,
//                     SUM(s.quantity_sold) AS total_quantity,
//                     SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit
//                 FROM sales s
//                 JOIN products p ON s.prod_id = p.prod_id
//                 GROUP BY p.prod_name
//                 ORDER BY SUM(s.quantity_sold) DESC
//                 FETCH FIRST :topCount ROWS ONLY
//             ", _connection);

//             cmd.Parameters.Add(new OracleParameter("topCount", topCount));

//             using var reader = await cmd.ExecuteReaderAsync();
//             var topProducts = new List<object>();

//             while (await reader.ReadAsync())
//             {
//                 var profit = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
//                 var profitPercent = totalSales == 0 ? 0 : Math.Round((profit / totalSales) * 100, 2);

//                 topProducts.Add(new
//                 {
//                     Product = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
//                     TotalQuantity = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
//                     Profit = profit,
//                     ProfitPercent = profitPercent
//                 });
//             }

//             return Ok(topProducts);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }


//     [HttpGet("top-products-in-top-countries/{countryCount?}/{productCount?}")]
//     public async Task<IActionResult> GetTopProductsInTopCountries(int countryCount = 3, int productCount = 3)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             using var countryCmd = new OracleCommand(@"
//                 SELECT co.country_id, co.country_name
//                 FROM sales s
//                 JOIN customers cu ON s.cust_id = cu.cust_id
//                 JOIN countries co ON cu.country_id = co.country_id
//                 GROUP BY co.country_id, co.country_name
//                 ORDER BY SUM(s.quantity_sold) DESC
//                 FETCH FIRST :topCount ROWS ONLY
//             ", _connection);

//             countryCmd.Parameters.Add(new OracleParameter("topCount", countryCount));

//             var topCountries = new List<(int Id, string Name)>();
//             using var reader = await countryCmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 topCountries.Add((
//                     Id: reader.GetInt32(0),
//                     Name: reader.GetString(1)
//                 ));
//             }

//             var result = new List<object>();

//             foreach (var country in topCountries)
//             {
//                 using var productCmd = new OracleCommand(@"
//                     SELECT p.prod_name, SUM(s.quantity_sold) AS total_quantity
//                     FROM sales s
//                     JOIN products p ON s.prod_id = p.prod_id
//                     JOIN customers cu ON s.cust_id = cu.cust_id
//                     WHERE cu.country_id = :countryId
//                     GROUP BY p.prod_name
//                     ORDER BY SUM(s.quantity_sold) DESC
//                     FETCH FIRST :productCount ROWS ONLY
//                 ", _connection);

//                 productCmd.Parameters.Add(new OracleParameter("countryId", country.Id));
//                 productCmd.Parameters.Add(new OracleParameter("productCount", productCount));

//                 var topProducts = new List<object>();
//                 using var productReader = await productCmd.ExecuteReaderAsync();
//                 while (await productReader.ReadAsync())
//                 {
//                     topProducts.Add(new
//                     {
//                         Product = productReader.GetString(0),
//                         Quantity = productReader.IsDBNull(1) ? 0 : productReader.GetInt32(1)
//                     });
//                 }

//                 result.Add(new
//                 {
//                     Country = country.Name,
//                     TopProducts = topProducts
//                 });
//             }

//             return Ok(result);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }

//    [HttpGet("sales-vs-promotions/{year}")]
//     public async Task<IActionResult> GetSalesVsPromotions(int year = 2022)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             string query = @"
//                 SELECT
//                     t.calendar_month_name AS month,
//                     p.promo_category,
//                     SUM(s.quantity_sold) AS category_sales,
//                     SUM(SUM(s.quantity_sold)) OVER (PARTITION BY t.calendar_year, t.calendar_month_name) AS total_sales
//                 FROM sales s
//                 JOIN promotions p ON s.promo_id = p.promo_id
//                 JOIN times t ON s.time_id = t.time_id
//                 WHERE t.calendar_year = :year
//                 GROUP BY t.calendar_year, t.calendar_month_name, t.calendar_month_desc, p.promo_category
//                 ORDER BY t.calendar_year, MIN(t.calendar_month_desc)
//             ";

//             using var cmd = new OracleCommand(query, _connection);
//             cmd.Parameters.Add(new OracleParameter("year", year));

//             var rawData = new List<(string Month, string Category, decimal CategorySales, decimal TotalSales)>();

//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 var month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
//                 var category = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
//                 var categorySales = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2));
//                 var totalSales = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3));

//                 rawData.Add((month, category, categorySales, totalSales));
//             }

//             var grouped = rawData
//                 .GroupBy(r => new { r.Month, r.TotalSales })
//                 .Select(g =>
//                 {
//                     var dict = new Dictionary<string, object>
//                     {
//                         ["month"] = g.Key.Month,
//                         ["totalSales"] = g.Key.TotalSales
//                     };

//                     foreach (var row in g)
//                     {
//                         var percent = g.Key.TotalSales == 0 ? 0 : Math.Round((row.CategorySales / g.Key.TotalSales) * 100, 2);
//                         dict[row.Category] = percent;
//                     }

//                     return dict;
//                 })
//                 .ToList();

//             return Ok(grouped);
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"ERROR in GetSalesVsPromotions: {ex}");
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }






//     [HttpGet("top-products-profit/{topCount?}")]
//     public async Task<IActionResult> GetTopProductsProfit(int topCount = 4)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             decimal totalSales = 0;
//             using (var totalCmd = new OracleCommand("SELECT SUM(amount_sold) FROM sales", _connection))
//             {
//                 totalSales = Convert.ToDecimal(await totalCmd.ExecuteScalarAsync());
//             }

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     p.prod_name AS product,
//                     SUM(s.amount_sold) AS total_sales,
//                     SUM(s.quantity_sold) AS total_quantity,      
//                     SUM(s.amount_sold) - SUM(s.quantity_sold * p.prod_min_price) AS profit
//                 FROM sales s
//                 JOIN products p ON s.prod_id = p.prod_id
//                 GROUP BY p.prod_name
//                 ORDER BY SUM(s.amount_sold) DESC
//                 FETCH FIRST :topCount ROWS ONLY
//             ", _connection);

//             cmd.Parameters.Add(new OracleParameter("topCount", topCount));

//             var result = new List<object>();
//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 var total = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
//                 var totalQuantity = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);  
//                 var profit = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
//                 var percentOfTotalSales = totalSales == 0 ? 0 : Math.Round((total / totalSales) * 100, 2);
//                 var profitPercent = total == 0 ? 0 : Math.Round((profit / total) * 100, 2);

//                 result.Add(new
//                 {
//                     Product = reader.GetString(0),
//                     TotalSales = total,
//                     TotalQuantity = totalQuantity,       
//                     Profit = profit,
//                     PercentOfTotalSales = percentOfTotalSales,
//                     ProfitPercent = profitPercent
//                 });
//             }

//             return Ok(result);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }



//     [HttpGet("sales-by-region-country")]
//     public async Task<IActionResult> GetSalesByRegionCountry()
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     co.country_region AS region,
//                     co.country_name AS country,
//                     SUM(s.quantity_sold) AS total_quantity
//                 FROM sales s
//                 JOIN customers cu ON s.cust_id = cu.cust_id
//                 JOIN countries co ON cu.country_id = co.country_id
//                 GROUP BY co.country_region, co.country_name
//                 ORDER BY co.country_region, co.country_name
//             ", _connection);

//             using var reader = await cmd.ExecuteReaderAsync();

//             var tempData = new Dictionary<string, Dictionary<string, int>>();

//             while (await reader.ReadAsync())
//             {
//                 string region = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
//                 string country = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
//                 int quantity = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

//                 if (!tempData.ContainsKey(region))
//                     tempData[region] = new Dictionary<string, int>();

//                 tempData[region][country] = quantity;
//             }

//             var result = tempData.Select(r => {
//                 var obj = new Dictionary<string, object> { ["region"] = r.Key };
//                 foreach (var kvp in r.Value)
//                 {
//                     obj[kvp.Key] = kvp.Value;
//                 }
//                 return obj;
//             }).ToList();

//             return Ok(result);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }

//     [HttpGet("sales-costs/{year}")]
//     public async Task<IActionResult> GetSalesAndCosts(int year)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     t.calendar_month_name AS month,
//                     SUM(s.amount_sold) AS total_sales,
//                     SUM(s.quantity_sold * p.prod_min_price) AS total_cost
//                 FROM sales s
//                 JOIN times t ON s.time_id = t.time_id
//                 JOIN products p ON s.prod_id = p.prod_id
//                 WHERE t.calendar_year = :year
//                 GROUP BY t.calendar_month_name, t.calendar_month_number
//                 ORDER BY t.calendar_month_number
//             ", _connection);

//             cmd.Parameters.Add(new OracleParameter("year", year));

//             using var reader = await cmd.ExecuteReaderAsync();
//             var salesCostData = new List<object>();

//             while (await reader.ReadAsync())
//             {
//                 salesCostData.Add(new
//                 {
//                     Month = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
//                     Amount_Sold = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
//                     Cost = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2)
//                 });
//             }

//             return Ok(salesCostData);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }

//     [HttpGet("top-customers/{topCount?}")]
//     public async Task<IActionResult> GetTopCustomers(int topCount = 5)
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             decimal totalSales = 0;
//             using (var totalCmd = new OracleCommand("SELECT SUM(amount_sold) FROM sales", _connection))
//             {
//                 totalSales = Convert.ToDecimal(await totalCmd.ExecuteScalarAsync() ?? 0);
//             }

//             using var cmd = new OracleCommand(@"
//                 SELECT 
//                     c.cust_email,
//                     SUM(s.amount_sold) AS sales
//                 FROM sales s
//                 JOIN customers c ON s.cust_id = c.cust_id
//                 GROUP BY c.cust_email
//                 ORDER BY SUM(s.amount_sold) DESC
//                 FETCH FIRST :topCount ROWS ONLY
//             ", _connection);

//             cmd.Parameters.Add(new OracleParameter("topCount", topCount));

//             var result = new List<object>();
//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 var custEmail = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
//                 var sales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);

//                 var percentOfTotal = totalSales == 0 ? 0 : Math.Round((sales / totalSales) * 100, 2);

//                 result.Add(new
//                 {
//                     cust_email = custEmail,
//                     sales = sales,
//                     percentOfTotal = percentOfTotal
//                 });
//             }

//             return Ok(result);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }


//    [HttpGet("sales-by-channel")]
//     public async Task<IActionResult> GetSalesByChannel()
//     {
//         try
//         {
//             await _connection.OpenAsync();

//             int totalQuantity = 0;
//             using (var totalCmd = new OracleCommand("SELECT SUM(quantity_sold) FROM sales", _connection))
//             {
//                 var result = await totalCmd.ExecuteScalarAsync();
//                 totalQuantity = result == DBNull.Value ? 0 : Convert.ToInt32(result);
//             }

//             using var cmd = new OracleCommand(@"
//                 SELECT
//                     ch.channel_desc AS channel,
//                     SUM(s.quantity_sold) AS total_quantity
//                 FROM sales s
//                 JOIN channels ch ON s.channel_id = ch.channel_id
//                 GROUP BY ch.channel_desc
//                 ORDER BY SUM(s.quantity_sold) DESC
//             ", _connection);

//             using var reader = await cmd.ExecuteReaderAsync();
//             var salesByChannel = new List<object>();

//             while (await reader.ReadAsync())
//             {
//                 var channel = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
//                 var qty = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
//                 var percent = totalQuantity == 0 ? 0 : Math.Round((double)qty / totalQuantity * 100, 2);

//                 salesByChannel.Add(new
//                 {
//                     Channel = channel,
//                     TotalQuantity = qty,
//                     PercentOfTotal = percent
//                 });
//             }

//             return Ok(salesByChannel);
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(500, new { error = ex.Message });
//         }
//         finally
//         {
//             await _connection.CloseAsync();
//         }
//     }

// }