using System.Globalization;
using System.Net;
using System.Text.Json;

var logFilePath = GetConfigValue("LOG_FILE_PATH", args, 0, "log.txt");
var outputFilePath = GetConfigValue("OUTPUT_FILE_PATH", args, 1, "output.txt");
var ipRangeStart = GetConfigValue("IP_RANGE_START", args, 2, "192.168.1.10");
var ipRangeEnd = GetConfigValue("IP_RANGE_END", args, 3, "192.168.1.100");
var startTime = GetConfigValue("START_TIME", args, 4, "2024-10-22 00:00:00");
var endTime = GetConfigValue("END_TIME", args, 5, "2024-10-23 23:59:59");

var startIP = IPAddress.Parse(ipRangeStart);
var endIP = IPAddress.Parse(ipRangeEnd);
var startDateTime = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
var endDateTime = DateTime.ParseExact(endTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

var ipCount = new Dictionary<IPAddress, int>();

foreach (var line in File.ReadLines(logFilePath))
{
    var separatorIndex = line.IndexOf(':');
    if (separatorIndex == -1)
    {
        Console.WriteLine("Некорректная строка, отсутствует двоеточие");
        continue;
    }

    var ipString = line[..separatorIndex];
    var timeString = line[(separatorIndex + 1)..].Trim();

    if (IPAddress.TryParse(ipString, out var ip) &&
        DateTime.TryParseExact(timeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var accessTime))
    {
        if (IsInRange(ip, startIP, endIP) && accessTime >= startDateTime && accessTime <= endDateTime)
        {
            if (ipCount.TryGetValue(ip, out var value))
                ipCount[ip] = ++value;
            else
                ipCount[ip] = 1;
        }
    }
}

using (var outputFile = new StreamWriter(outputFilePath))
{
    foreach (var entry in ipCount)
        outputFile.WriteLine($"{entry.Key} - {entry.Value} обращений");
}

Console.WriteLine("Результаты успешно сохранены в файл.");
return;

static bool IsInRange(IPAddress ip, IPAddress start, IPAddress end)
{
    var ipBytes = ip.GetAddressBytes();
    var startBytes = start.GetAddressBytes();
    var endBytes = end.GetAddressBytes();

    bool lowerBound = true, upperBound = true;

    for (var i = 0; i < ipBytes.Length && (lowerBound || upperBound); i++)
    {
        if ((lowerBound && ipBytes[i] < startBytes[i]) || (upperBound && ipBytes[i] > endBytes[i]))
            return false;

        lowerBound &= ipBytes[i] == startBytes[i];
        upperBound &= ipBytes[i] == endBytes[i];
    }

    return true;
}

static Dictionary<string, string>? LoadConfig(string configFilePath)
{
    if (!File.Exists(configFilePath)) return null;
    var configJson = File.ReadAllText(configFilePath);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
}


static string GetConfigValue(string envVar, string[] args, int argIndex, string defaultValue)
{
    if (args.Length > argIndex)
    {
        return args[argIndex];
    }

    var config = LoadConfig("config.json");
    if (config != null && config.TryGetValue(envVar, out var configValue))
        return configValue;

    return defaultValue;
}