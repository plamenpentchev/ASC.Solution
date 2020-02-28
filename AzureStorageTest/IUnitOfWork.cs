using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageTest
{
    public interface IUnitOfWork: IDisposable
    {
         string ConnectionString { get; set; }
         Queue<Task<Action>> RollbackActions { get; set; }
        IRepository<T> Repository<T>() where T : TableEntity;

        void CommitTransactions();
    }
}
