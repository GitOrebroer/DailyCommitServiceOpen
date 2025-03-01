### How the Daily Commit Application Works

The **Daily Commit Application** consists of two main parts:

1. **CommiterHostedService** (Background Service):
    - This service runs periodically, fetching commit data from Google Firestore and using GitHub to create commits based on that data.
    - It uses `FirestoreDb` to interact with Firestore and fetch documents from the `dailyCommits` collection.
    - Each document represents a user, and it contains commit data like `commitDate`, `interval`, `uuid`, `shaHash`, and others.
    - The service checks for active daily commits, performs random checks for commit days, and verifies user token validity.
    - It then uses `CreateCommitUseCase` to create commits in GitHub repositories.

2. **CreateCommitUseCase** (Use Case):
    - This is where the actual commit is made to a GitHub repository.
    - The `CreateCommitUseCase` interacts with the GitHub API through the `Octokit` library to create commits.
    - It uses the `UpdateFileRequest` to update the `readme.txt` file with a commit message containing the current date and time.
    - The process attempts multiple commits based on the `interval` specified, and handles potential errors like SHA conflicts or repository issues.
    - The result is returned, containing success or failure status and any relevant error information.

### **Workflow Overview:**
1. **Fetch Data**: The service fetches the daily commit data from Firestore.
2. **Check Validity**: It checks if the commit is valid based on conditions like active days, token validity, and other data.
3. **Commit to GitHub**: The service makes commits on GitHub repositories by calling the `CreateCommitUseCase`.
4. **Error Handling**: The system handles errors such as invalid tokens, SHA conflicts, and repository errors.

This approach ensures that commits are made periodically for active users, based on the data stored in Firestore and GitHub credentials.
