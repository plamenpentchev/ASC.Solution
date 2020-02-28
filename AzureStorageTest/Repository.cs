using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageTest
{
    public class Repository<T> : IRepository<T> where T : TableEntity, new()
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly CloudTableClient tableClient;
        private readonly CloudTable storageTable;

        public IUnitOfWork Scope { get; set; }

        public Repository(IUnitOfWork scope)
        {
            this.storageAccount = CloudStorageAccount.Parse(scope.ConnectionString);
            this.tableClient = storageAccount.CreateCloudTableClient();
            this.storageTable = tableClient.GetTableReference(typeof(T).Name);
            this.Scope = scope;
        }

        public async Task<T> AddAsync(T entity)
        {
            var entityToInsert = entity as BaseEntity;
            entityToInsert.CreatedDate = DateTime.UtcNow;
            entityToInsert.UpdatedDate = DateTime.UtcNow;

            TableOperation op = TableOperation.Insert(entity);
            var result =  await ExecuteAsync(op);
            return result.Result as T;

        }

        public async Task<T> UpdateAsync(T entity)
        {
            var entityToUpdate = entity as BaseEntity;
            entityToUpdate.UpdatedDate = DateTime.UtcNow;

            TableOperation op = TableOperation.Replace(entity);
            var result = await this.ExecuteAsync(op);
            return result.Result as T;
        }

        public async Task DeleteAsync(T entity)
        {
            var entityToDelete = entity as BaseEntity;
            entityToDelete.UpdatedDate = DateTime.UtcNow;
            entityToDelete.IsDeleted = true;

            await ExecuteAsync(TableOperation.Replace(entityToDelete));
        }

        public async Task<T> FindAsync(string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await ExecuteAsync(retrieveOperation);
            return result.Result as T;
        }

        public async Task<IEnumerable<T>> FindAllByPartitionKeyAsync(string partitionKey)
        {
            TableQuery<T> query = new TableQuery<T>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken tableContinuationToken = null;
            var result = await this.storageTable.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
            return result.Results as IEnumerable<T>;
        }

        public async Task<IEnumerable<T>> FindAllAsync()
        {
            TableQuery query = new TableQuery();
            TableContinuationToken tableContinuationToken = null;

            var result = await this.storageTable.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
            return result.Results as IEnumerable<T>;
        }

        public async Task CreateTableAsync()
        {
            CloudTable table =  this.tableClient.GetTableReference(typeof(T).Name);
            await table.CreateIfNotExistsAsync();
            if (typeof(IAuditTracker).IsAssignableFrom(typeof(T)))
            {
                var auditTable = this.tableClient.GetTableReference($"{typeof(T).Name}Audit");
                await auditTable.CreateIfNotExistsAsync();
            }
        }

       
        /// <summary>
        /// custom rollback logic
        /// performs delete for a Create operation
        ///         sets IsDeleted flag to false for Delete operation
        ///         replaces the updated entity with the original entity for Update operation
        /// </summary>
        /// <returns></returns>
        private async Task<Action> CreateRollbackAction(TableOperation operation)
        {
            if (operation.OperationType == TableOperationType.Retrieve) return null;

            var cloudTable = this.storageTable;
            var tableEntity = operation.Entity;
            switch (operation.OperationType)
            {
                case TableOperationType.Insert:
                    return async () => await UndoInsertOperationAsync(cloudTable, tableEntity);
                case TableOperationType.Delete:
                    return async () => await UndoDeleteOperationAsync(cloudTable, tableEntity);
                case TableOperationType.Replace:
                    var retrieveResult 
                        = await cloudTable.ExecuteAsync(TableOperation.Retrieve(tableEntity.PartitionKey, tableEntity.RowKey));
                    return async () => await UndoReplaceOperation(cloudTable, retrieveResult.Result as DynamicTableEntity, tableEntity);

                default:
                    throw new InvalidOperationException($"The storage operation" +
                        $" '{operation.OperationType}' could not be identified.");
            }
        }

        private async Task UndoReplaceOperation(CloudTable cloudTable, DynamicTableEntity originalEntity, ITableEntity newEntity)
        {
            if (null != originalEntity)
            {
                if (!String.IsNullOrEmpty(newEntity.ETag))
                {
                    originalEntity.ETag = newEntity.ETag;
                }

                await cloudTable.ExecuteAsync(TableOperation.Replace(originalEntity));
            }
            
        }

        private async Task UndoDeleteOperationAsync(CloudTable cloudTable, ITableEntity tableEntity)
        {
            var entityToRestore = tableEntity as BaseEntity;
            entityToRestore.IsDeleted = false;

             await cloudTable.ExecuteAsync(TableOperation.Replace(tableEntity));
        }

        private async Task UndoInsertOperationAsync(CloudTable cloudTable, ITableEntity tableEntity)
            => await cloudTable.ExecuteAsync(TableOperation.Delete(tableEntity));


        private async Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            var rollbackAction = CreateRollbackAction(operation);
            var result = await this.storageTable.ExecuteAsync(operation);
            this.Scope.RollbackActions.Enqueue(rollbackAction);

            return result;
        }

    }
}
