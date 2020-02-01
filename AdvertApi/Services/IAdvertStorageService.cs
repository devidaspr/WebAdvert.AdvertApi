using AdvertApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdvertApi.Services
{
    public interface IAdvertStorageService
    {
        Task<string> AddAsync(AdvertModel model);
        Task ConfirmAsync(ConfirmAdvertModel model);
        Task<bool> CheckHealthAsync();
        Task<AdvertModel> GetByIdAsync(string Id);
        Task<List<AdvertModel>> GetAllAsync();
    }
}
