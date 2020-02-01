using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvertApi.Models;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AdvertApi.Services
{
    public class DynamoDBAdvertStorage : IAdvertStorageService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<DynamoDBAdvertStorage> _logger;

        public DynamoDBAdvertStorage(IMapper mapper, ILogger<DynamoDBAdvertStorage> logger)
        {
            this._mapper = mapper;
            this._logger = logger;
        }

        public async Task<string> AddAsync(AdvertModel model)
        {
            var dbModel = _mapper.Map<AdvertDbModel>(model);

            dbModel.Id = Guid.NewGuid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;

            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    await context.SaveAsync(dbModel);
                }
            }

            return dbModel.Id;
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                //using (var client = new AmazonDynamoDBClient(new StoredProfileAWSCredentials(),
                //      RegionEndpoint.USWest2))
                using (var client = new AmazonDynamoDBClient())
                {
                    var tableData = await client.DescribeTableAsync("Adverts");
                    return string.Compare(tableData.Table.TableStatus, "active", true) == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while checing connection with DynamoDB table. Message:{ex.Message}");
                throw;
            }
        }

        public async Task ConfirmAsync(ConfirmAdvertModel model)
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var record = await context.LoadAsync<AdvertDbModel>(model.Id);
                    if (record == null)
                    {
                        throw new KeyNotFoundException($"A record with Id={model.Id} was not found");
                    }
                    if (model.Status == AdvertStatus.Active)
                    {
                        record.FilePath = model.FilePath;
                        record.Status = AdvertStatus.Active;
                        await context.SaveAsync(record);
                    }
                    else 
                    {
                        await context.DeleteAsync(record);
                    }
                }
            }
        }

        public async Task<List<AdvertModel>> GetAllAsync()
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    //ScanAsync can be used for demo however it cannot be used for production
                    //for production first create an index and run queries on that index
                    var allItems = await context.ScanAsync<AdvertDbModel>(
                                                    new List<ScanCondition>()).GetRemainingAsync();

                    return allItems.Select(item => _mapper.Map<AdvertModel>(item)).ToList();
                }
            }
        }

        public async Task<AdvertModel> GetByIdAsync(string Id)
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var dbModel = await context.LoadAsync<AdvertDbModel>(Id);
                    if (dbModel != null)
                        return _mapper.Map<AdvertModel>(dbModel);
                }
            }

            throw new KeyNotFoundException();
        }
    }
}
