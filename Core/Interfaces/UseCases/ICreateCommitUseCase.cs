using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.UseCases
{
    public interface ICreateCommitUseCase
    {
        Task<Result<string>> ExecuteUseCaseAsync(string owner, string repoName, string path, string ghToken, string shaHash, int interval);
    }
}
