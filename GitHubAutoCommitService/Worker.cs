using System.Diagnostics;
using System.Text.Json;

namespace GitHubAutoCommitService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan interval = TimeSpan.FromHours(3);

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Run();

                await Task.Delay(interval, stoppingToken);
            }
        }

        protected void Run()
        {
            _logger.LogInformation("GitHub AutoCommitter running, started at: {time}", DateTimeOffset.Now);
            
            var repoPath = _configuration.GetSection("GitRepoPath").Value;
            var historyFilePath = _configuration.GetSection("HistoryFilePath").Value;

            DateTime? lastRun = GetLastRun(historyFilePath);

            if (repoPath is null)
            {
                throw new InvalidDataException("Unable to find path to git repo");
            }

            if (lastRun is null)
            {
                _logger.LogWarning($"Encountered a null value for lastRun! LastRun: {lastRun}");
                _logger.LogInformation("Immediately committing");
            }

            if (WasLastRunInThePast24Hours(lastRun))
            {
                _logger.LogInformation("Last run was within the past 24 hours, skipping");
                return;
            }

            try
            {
                _logger.LogInformation("Starting commit...");

                Commit(repoPath);
                WriteToFile(historyFilePath);

                _logger.LogInformation("Committed successfully and updated last run date.");

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error when trying to commit to Git Hub.");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GitHub Auto Committer service shutting down...");
            return Task.CompletedTask;
        }

        //Virtual methods here are a way to allow mocking in unit tests

        protected virtual void WriteToFile(string historyFilePath)
        {
            if (string.IsNullOrEmpty(historyFilePath)) return;

            File.WriteAllText(historyFilePath, JsonSerializer.Serialize(DateTime.Now));
        }

        protected virtual void Commit(string repoPath)
        {
            ExecuteGitCommand("git", "add .", repoPath);
            ExecuteGitCommand("git", "commit -m \"auto commit via windows service\"", repoPath);
            ExecuteGitCommand("git", "push origin master", repoPath);
        }

        private void ExecuteGitCommand(string exe, string args, string repoPath)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process is not null) process.WaitForExit();
            _logger.LogInformation($"Executed command successfully: {exe} {args}");
        }

        protected virtual DateTime? GetLastRun(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var lastRuntimeStr = File.ReadAllText(path);

            if (string.IsNullOrEmpty(lastRuntimeStr)) return null;

            var lastRuntime = (DateTime)JsonSerializer.Deserialize(lastRuntimeStr, typeof(DateTime));

            // not super bullet-proof but the goal is to check if the date was deserialized into a valid date
            if(lastRuntime.Year != DateTime.Now.Year || lastRuntime.Year - 1 != DateTime.Now.Year - 1)
            {
                _logger.LogCritical($"Unable to deserialize last run time to valid DateTime. Year is not current year or past year. Parsed Date: {lastRuntime.ToString()}");
                return null;
            }

            return lastRuntime;
        }

        private bool WasLastRunInThePast24Hours(DateTime? lastRunTime)
        {
            if (lastRunTime is null) return false;

            return DateTime.Now.Subtract(TimeSpan.FromDays(1)) < lastRunTime;
        }
    }
}
