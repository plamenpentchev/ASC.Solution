using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageTest
{
    class UnitOfWork : IUnitOfWork
    {

        private bool completed = false;
        private bool disposed = false;
        private Dictionary<string, object> _repositories;

        public string ConnectionString{ get; set; }
       

        public UnitOfWork(string connectionString)
        {
            this.ConnectionString = connectionString;
            this.RollbackActions = new Queue<Task<Action>>();
        }
        
        public Queue<Task<Action>> RollbackActions { 
            get ; 
            set ; 
        }

        public void CommitTransactions()
        {
            this.completed = true;
        }
        ~UnitOfWork()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (!this.completed)
                    {
                        this.RollbackTransaction();
                    }
                }
                finally 
                {
                    this.RollbackActions.Clear();
                }
            }
       
            this.completed = false;
        }

        public IRepository<T> Repository<T>() where T : TableEntity
        {
            if (this._repositories == null) this._repositories = new Dictionary<string, object>();

            var type = typeof(T).Name;
            if (this._repositories.ContainsKey(type)) return (IRepository<T>)this._repositories[type];

            var repositoryType = typeof(Repository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), this);

            this._repositories.Add(type, repositoryInstance);
            return (IRepository<T>)this._repositories[type];
        }

        private void RollbackTransaction()
        {
            while (this.RollbackActions.Count > 0)
            {
                var undoAction = this.RollbackActions.Dequeue();
                if(null != undoAction && null != undoAction.Result) undoAction.Result();
            }
        }
    }
}
