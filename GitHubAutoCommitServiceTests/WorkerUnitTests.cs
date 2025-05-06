using GitHubAutoCommitService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace GitHubAutoCommitServiceTests
{
    public class TestWorker : Worker
    {
        public DateTime LastRunDateTime { get; set; }
        public int CommitCounter = 0;
        public int WriteToFileCounter = 0;

        public TestWorker(ILogger<Worker> logger, IConfiguration configuration, DateTime lastRunDateTime) : base(logger, configuration)
        {
            LastRunDateTime = lastRunDateTime;
        }

        protected override void WriteToFile(string historyFilePath)
        {
            WriteToFileCounter += 1;
        }

        protected override DateTime? GetLastRun(string path)
        {
            return this.LastRunDateTime;
        }

        protected override void Commit(string repoPath)
        {
            CommitCounter += 1;
        }
    }

    public class WorkerUnitTests
    {
        [Fact]
        public async Task Service_Does_Not_Commit_If_Committed_In_The_Last_24_Hours()
        {
            var mockLogger = new Mock<ILogger<Worker>>();
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(mc => mc.GetSection("GitRepoPath").Value).Returns(() => "C:/some/repo/path");
            mockConfig.Setup(mc => mc.GetSection("HistoryFilePath").Value).Returns(() => "C:/some/file/path");

            var testWorker = new TestWorker(mockLogger.Object, mockConfig.Object, DateTime.Now);
            
            var exception = await Record.ExceptionAsync(() => testWorker.StartAsync(default));

            Assert.Null(exception);
            Assert.True(testWorker.CommitCounter == 0);
        }

        [Fact]
        public async Task Service_Does_Not_Commit_If_RepoPath_is_null()
        {
            var mockLogger = new Mock<ILogger<Worker>>();
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(mc => mc.GetSection("GitRepoPath").Value).Returns(() => null);
            mockConfig.Setup(mc => mc.GetSection("HistoryFilePath").Value).Returns(() => "C:/some/file/path");

            var testWorker = new TestWorker(mockLogger.Object, mockConfig.Object, DateTime.Now);

            var exception = await Record.ExceptionAsync(() => testWorker.StartAsync(default));

            Assert.Null(exception);
            Assert.True(testWorker.CommitCounter == 0);
        }

        [Fact]
        public async Task Service_Does_Commits_And_Writes_To_File()
        {
            var mockLogger = new Mock<ILogger<Worker>>();
            var mockConfig = new Mock<IConfiguration>();

            mockConfig.Setup(mc => mc.GetSection("GitRepoPath").Value).Returns(() => "C:/some/repo/path");
            mockConfig.Setup(mc => mc.GetSection("HistoryFilePath").Value).Returns(() => "C:/some/file/path");

            var testWorker = new TestWorker(mockLogger.Object, mockConfig.Object, DateTime.Now.Subtract(TimeSpan.FromHours(48)));

            var exception = await Record.ExceptionAsync(() => testWorker.StartAsync(default));

            Assert.Null(exception);
            Assert.True(testWorker.CommitCounter == 1);
            Assert.True(testWorker.WriteToFileCounter == 1);
        }
    }
}