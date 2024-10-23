using DeliveryService.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryService.Controllers;

[Route("api/[controller]")]
public class OrdersController(IConfiguration configuration) : ControllerBase
{
    private static readonly List<Order> Orders = [];

    [HttpPost("load")]
    public IActionResult LoadOrdersFromFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Файл не может быть пустым" });


        try
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.ReadLine() is { } line)
                {
                    var parts = line.Split(';');

                    if (parts.Length != 3)
                        return BadRequest(new
                            { message = "Некорректный формат данных. Ожидается три значения через ';'" });


                    if (!double.TryParse(parts[0], out var weight) || weight <= 0)
                        return BadRequest(new { message = $"Некорректный вес в строке: {line}" });


                    if (string.IsNullOrWhiteSpace(parts[1]))
                        return BadRequest(new { message = $"Некорректный район в строке: {line}" });


                    if (!DateTime.TryParse(parts[2], out var deliveryTime))
                        return BadRequest(new { message = $"Некорректная дата в строке: {line}" });


                    Orders.Add(new Order
                    {
                        OrderId = Guid.NewGuid(),
                        Weight = weight,
                        Area = parts[1],
                        DeliveryTime = deliveryTime
                    });
                }
            }

            Log("Заказы загружены успешно!");
            return Ok(new { message = "Заказы загружены успешно!" });
        }
        catch (IOException ex)
        {
            return StatusCode(500, new { message = $"Ошибка ввода-вывода: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Неожиданная ошибка: {ex.Message}" });
        }
    }

    [HttpGet("filter")]
    public IActionResult FilterOrders(string area, DateTime startTime, DateTime endTime)
    {
        if (string.IsNullOrWhiteSpace(area))
            return BadRequest(new { message = "Район не может быть пустым" });

        if (startTime > endTime)
            return BadRequest(new { message = "Время начала должно быть меньше времени окончания" });

        var filteredOrders = Orders
            .Where(o => o.Area.Equals(area, StringComparison.OrdinalIgnoreCase) && o.DeliveryTime >= startTime &&
                        o.DeliveryTime <= endTime)
            .ToList();

        if (filteredOrders.Count == 0)
        {
            Log($"Заказы не найдены для района: {area} в указанный интервал времени.");
            return NotFound(new { message = "Заказы не найдены" });
        }
        
        var firstOrderTime = filteredOrders.Min(o => o.DeliveryTime);
        var targetTime = firstOrderTime.AddMinutes(30);
        
        var finalFilteredOrders = filteredOrders
            .Where(o => o.DeliveryTime <= targetTime)
            .ToList();

        SaveFilteredOrders(finalFilteredOrders);
        return Ok(finalFilteredOrders);
    }

    private void Log(string message)
    {
        var logMessage = $"{DateTime.Now}: {message}";
        System.IO.File.AppendAllText(configuration["LogFilePath"]!, logMessage + Environment.NewLine);
    }

    private void SaveFilteredOrders(List<Order> filteredOrders)
    {
        using var writer = new StreamWriter(configuration["ResultFilePath"]!, append: true);
        foreach (var order in filteredOrders)
        {
            writer.WriteLine($"{order.OrderId};{order.Weight};{order.Area};{order.DeliveryTime}");
        }
    }
}