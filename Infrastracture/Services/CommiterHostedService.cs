using Core.Interfaces;
using Core.Interfaces.UseCases;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Infrastracture.Services
{
    public class CommiterHostedService : BackgroundService
    {
        FirestoreDb db;
        // if interval is changed / azure function refresher also needs to be changed
        private readonly TimeSpan _intervalOfFetchingData = TimeSpan.FromMinutes(59);
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILoggerHelper logger;
        private CancellationTokenSource _cancellationTokenSource;
        public CommiterHostedService(IServiceScopeFactory serviceScopeFactory, ILoggerHelper logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Log($"CommiterHostedService was STARTed at:{DateTime.UtcNow} utcnow", false);
            db = FirestoreDb.Create("ADD YOUR CREDENTIALS");

            _cancellationTokenSource = new CancellationTokenSource();

            await ManipulateDailyCommitsPeriodically(_cancellationTokenSource.Token);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            logger.Log($"CommiterHostedService was STOPPed at:{DateTime.UtcNow} utcnow", false);
            return Task.CompletedTask;
        }

        private async Task ManipulateDailyCommitsPeriodically(CancellationToken cancellationToken)
        {
            try
            {
                using (IServiceScope scope = serviceScopeFactory.CreateScope())
                {
                    var createCommitUsecase =
                scope.ServiceProvider.GetRequiredService<ICreateCommitUseCase>();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(5000, cancellationToken);

                        List<DocumentSnapshot> cachedDocuments = new List<DocumentSnapshot>();

                        var snapshot = await db.Collection("dailyCommits").GetSnapshotAsync();
                        cachedDocuments.AddRange(snapshot.Documents);

                        var sortedDocs = cachedDocuments.Where(x => x.TryGetValue("commitDate", out DateTime dateOfCommit) && dateOfCommit < DateTime.UtcNow.Date).ToList();
                        // if u want to run it every-hour make  cachedDocuments.Where (if once a day sortedDocs.Where )
                        var cachedDoc = sortedDocs.Where(x => x.TryGetValue("isActive", out bool hasActiveDailyCommit) && hasActiveDailyCommit == true).ToList();

                        logger.Log($"About to begin logging found: {cachedDoc.Count} Cached active documents. Current date: { DateTime.UtcNow.Date}", false);

                        foreach (DocumentSnapshot document in cachedDoc)
                        {
                            bool hasInterval = document.TryGetValue("interval", out int interval);
                            //document.TryGetValue("commitDate", out  Timestamp commitDate);
                            bool hasUuid = document.TryGetValue("uuid", out string uuid);
                            //document.TryGetValue("ghtoken", out string ghTokenTest); this is not used anymore because we get the ghToken from users collection
                            bool hasShaHash = document.TryGetValue("shaHash", out string shaHash);
                            bool hasRepoName = document.TryGetValue("repoName", out string repoName);
                            bool hasUserName = document.TryGetValue("username", out string username);
                            bool hasRandomDaysActivated = document.TryGetValue("randomDays", out bool randomDays);

                            if (!hasInterval || !hasUuid || !hasShaHash || !hasRepoName || !hasUserName)
                            {
                                logger.Log($"/CommiterHosedService/ One of the basic needed values is missing Interval: {hasInterval}, uuid: {hasUuid}, shaHash: {hasShaHash}, repoName: {hasRepoName}, userName:{hasUserName}", true);
                                continue;
                            }

                            if (interval > 30)
                            {
                                logger.Log($"/CommiterHosedService/ Too big interval detected: {interval}, Uuid: {uuid}, Username: {username}", true);
                                continue;
                            }

                            var random = new Random();
                            var randomInterval = random.Next(1, interval + 1);

                            if (hasRandomDaysActivated)
                            {
                                var currentDay = DateTime.Now.DayOfWeek;

                                if (currentDay  == DayOfWeek.Monday)
                                {
                                    var numberOfDays = random.Next(1, 8);

                                    var setOfDays = new List<int>();

                                    if (numberOfDays == 7)
                                    {
                                        setOfDays = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
                                    }
                                    else
                                    {
                                        for (int i = 1; i <= numberOfDays; i++)
                                        {
                                            var randomDayInt = random.Next(1, 8);

                                            while(setOfDays.Contains(randomDayInt))
                                            {
                                                randomDayInt = random.Next(1, 8);
                                            }

                                            setOfDays.Add(randomDayInt);
                                        }
                                    }

                                    var documentReference = document.Reference;
                                    Dictionary<string, object> updates = new Dictionary<string, object>
                                        {
                                            {"commitDays", setOfDays},
                                        };

                                    await documentReference.UpdateAsync(updates);

                                    if (!setOfDays.Contains((int)currentDay))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    bool hasCurrentActiveDays = document.TryGetValue("commitDays", out List<int> currentActiveDays);

                                    if (hasCurrentActiveDays)
                                    {
                                        if (!currentActiveDays.Contains((int)currentDay))
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }

                            var userSnapshot = await db.Document($"users/{uuid}").GetSnapshotAsync();

                            if (!userSnapshot.Exists)
                            {
                                var documentReference = document.Reference;
                                await documentReference.UpdateAsync("isActive", false);
                                logger.Log($"/CommiterHosedService/ userSnapshot does not exist for user: {uuid}, {username}", true);
                                continue;
                            }

                            userSnapshot.TryGetValue("ghtoken", out string ghToken);
                            var hasIsTokenActive = userSnapshot.TryGetValue("isTokenValid", out bool isTokenActive);

                            if (!hasIsTokenActive || !isTokenActive)
                            {
                                var documentReference = document.Reference;
                                await documentReference.UpdateAsync("isActive", false);
                                continue;
                            }

                            var result = await createCommitUsecase.ExecuteUseCaseAsync(username, repoName, "readme.txt", ghToken, shaHash, randomInterval);

                            if (result.Success)
                            {
                                // if result is success then update firebase 
                                var documentReference = document.Reference;
                                Dictionary<string, object> updates = new Dictionary<string, object>
                                {
                                    {"shaHash", result.Value! },
                                    {"commitDate", DateTime.UtcNow }
                                };

                                logger.Log("Commit created for user:" + username + " UUID: " + uuid,false);

                                await documentReference.UpdateAsync(updates);
                            }

                            if (!result.Success && result.HttpStatusCode == 409)
                            {
                                // sha not valid
                                logger.Log($"/CommiterHosedService/409/Sha Not Valid/ User:{username} token: {ghToken} repoName: {repoName} shaHash: {shaHash} Error:{result.Error} ", true);
                                var documentReference = document.Reference;
                                await documentReference.UpdateAsync("shaHash", result.Value);
                            }
                            else if (!result.Success && result.HttpStatusCode == 404)
                            {
                                // Repo not found
                                logger.Log($"/CommiterHosedService/404/RepoNotFound/ User:{username} token: {ghToken} repoName: {repoName} Error:{result.Error} ", true);
                                var documentReference = document.Reference;
                                await documentReference.UpdateAsync("isActive", false);
                            }
                            else if (!result.Success && result.HttpStatusCode == 500)
                            {
                                logger.Log($"/CommiterHosedService/500/ Internal Server Error/ User:{username} token: {ghToken} Error:{result.Error} ", true);
                            }
                            else if (!result.Success && result.HttpStatusCode == 401)
                            {
                                // UInauthorized when gh token invalid
                                logger.Log($"/CommiterHosedService/404/ User unathorized: {username} token: {ghToken} Error:{result.Error} ", true);
                                var documentReference = document.Reference;
                                var userDocumentReference = userSnapshot.Reference;
                                await userDocumentReference.UpdateAsync("isTokenValid", false);
                                //await documentReference.UpdateAsync("isActive", false); if is active then 
                            }
                            else if (!result.Success)
                            {
                                logger.Log($"/CommiterHosedService/Unknown unhandled error/ User: {username} token: {ghToken} Error:{result.Error} ", true);
                            }

                        }

                        await Task.Delay(_intervalOfFetchingData, cancellationToken);
                    }

                }
            }
            catch (TaskCanceledException ex)
            {
                // Task was cancelled, exit the loop
                logger.Log($"Task Canceled Exception in CommiterHostedService {ex.ToString()}", true);
            }
            catch (Exception ex)
            {

                // LOG WHEN EXCEPTION HAPPENS AND INCLUDE DOCUMENT ID
                logger.Log($"Critical Exception in CommiterHostedService Message: {ex.Message} Exception:{ex.ToString()} ", true);
                Console.WriteLine($"An error occurred: Message: {ex.Message} Exception: {ex.ToString()}");
            }
        }


    }
}
