namespace TgTaskBot
{
    internal static class Config
    {
        public static string SqlConnectionString
        {
            get
            {
                string dbName = Environment.GetEnvironmentVariable("DB_NAME");
                string dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
                string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");               

                return $"Host=localhost;Port=5432;Database={dbName};Username={dbUsername};Password={dbPassword}";
            }
        }        
    }
}
