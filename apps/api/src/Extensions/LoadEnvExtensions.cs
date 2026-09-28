namespace API.Extensions;

public static class LoadEnvExtension
{
    public static WebApplicationBuilder LoadEnv(this WebApplicationBuilder builder)
    {
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");

        if (File.Exists(dotenv))
        {
            foreach (var line in File.ReadAllLines(dotenv))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;

                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }

        builder.Configuration.AddEnvironmentVariables();
        
        return builder;
    }
}