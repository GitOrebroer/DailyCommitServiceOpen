using Core.Interfaces;
using Core.Interfaces.UseCases;
using Core.Models;
using Octokit;

namespace Core.UseCases
{
    public class CreateCommitUseCase : ICreateCommitUseCase
    {
        private readonly ILoggerHelper logger;
        GitHubClient client;
        IRepositoryContentsClient repoContentClient;
        
        public CreateCommitUseCase(ILoggerHelper logger)
        {
            var connection = new Connection(new ProductHeaderValue("ADD YOUR CREDENTIALS"));
             client = new GitHubClient(connection);
            this.logger = logger;
        }

        public async Task<Result<string>> ExecuteUseCaseAsync(string owner,string repoName,string path, string ghToken,string shaHash, int interval)
        {
            try
            {
                logger.Log("Initiating commit for: " + owner,false);
                var tokenAuth = new Credentials(ghToken); // This can be a PAT or an OAuth token.
                client.Credentials = tokenAuth;
                client.Connection.Credentials = tokenAuth;

                repoContentClient = client.Repository.Content;

                string? nextSha = null;

                for (int i = 0; i < interval; i++)
                {
                    await Task.Delay(2000);

                    DateTime date = DateTime.Now;
                    var unixEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    var formatedDate = date.ToString("dddd, MMMM dd, yyyy HH:mm:ss.fff");

                    var content = $"DailyCommit Bot, your most recent commit to this file was made at: {formatedDate} UTC";
                    var updateFileRequest = new UpdateFileRequest(unixEpoch.ToString(), content, nextSha != null ? nextSha : shaHash);

                    var result = await repoContentClient.UpdateFile(owner, repoName, path, updateFileRequest);

                    nextSha = result?.Content?.Sha;
                }

                return new Result<string>()
                {
                    HttpStatusCode = 201,
                    Success = true,
                    Value = nextSha
                };

            }
            catch (ApiException ex) // do not catch httprequestexception
            {
                if (ex.Message.Contains($"does not match {shaHash}"))
                {
                    // if there was conflict with sha 
                    var readmeContent = await repoContentClient.GetAllContents(owner, repoName, "readme.txt");
                    var newSha = readmeContent?.First()?.Sha;

                    return new Result<string>()
                    {
                        HttpStatusCode = 409,
                        Success = false,
                        Error = $"CreateCommitUseCase/ExecuteUseCaseAsync Exception:" + ex.ToString(),
                        Value = newSha
                    };
                }

                return new Result<string>()
                {
                    HttpStatusCode = (int)ex.StatusCode,
                    Success = false,
                    Error = $"CreateCommitUseCase/ExecuteUseCaseAsync Exception:" + ex.ToString(),
                    Value = null
                };
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
                return new Result<string>()
                {
                    HttpStatusCode = 500,
                    Success = false,
                    Error = $"CreateCommitUseCase/ExecuteUseCaseAsync Exception:" + ex.ToString()
                };
            }

        }
    }
}



