using Microsoft.Extensions.Caching.Distributed;
using MvcCoreElastiCacheAWS.Helpers;
using MvcCoreElastiCacheAWS.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MvcCoreElastiCacheAWS.Services
{
    public class ServiceAWSCache
    {
        private IDistributedCache cache;

        public ServiceAWSCache(IDistributedCache cache)
        {
            this.cache = cache;
        }

        public async Task<List<Coche>> GetCochesFavoritosAsync()
        {
            // ALMACENAREMOS UNA COLECCIÓN DE COCHES EN FORMATO JSON
            // LAS KEYS DEBEN SER ÚNICAS PARA CADA USER
            string jsonCoches = await this.cache.GetStringAsync("cochesfavoritos");
            if (jsonCoches == null)
            {
                return null;
            }
            else
            {
                List<Coche> cars = JsonConvert.DeserializeObject<List<Coche>>(jsonCoches);
                return cars;
            }
        }

        public async Task AddCocheFavoritoAsync(Coche car)
        {
            List<Coche> coches = await this.GetCochesFavoritosAsync();
            // SI NO EXISTEN COCHES FAVORITOS TODAVÍA,
            // CREAMOS LA COLECCIÓN
            if (coches == null)
            {
                coches = new List<Coche>();
            }
            // AÑADIMOS EL NUEVO COCHE A LA COLECCIÓN
            coches.Add(car);
            // SERIALIZAMOS A JSON LA COLECCIÓN
            string jsonCoches = JsonConvert.SerializeObject(coches);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            // ALMACENAMOS LA COLECCIÓN DENTRO DE CACHE REDIS
            // INDICAREMOS QUE LOS DATOS DURARÁN 30 MIN
            await this.cache.SetStringAsync("cochesfavoritos", jsonCoches, options);
        }

        public async Task DeleteCocheFavoritoAsync(int idcoche)
        {
            List<Coche> cars = await this.GetCochesFavoritosAsync();
            if (cars != null)
            {
                Coche cocheEliminar = cars.FirstOrDefault(c => c.IdCoche == idcoche);
                cars.Remove(cocheEliminar);
                // COMPROBAMOS SI LA COLECCIÓN TIENE COCHES FAVORITOS
                // TODAVÍA O NO TIENE
                // SI NO TENEMOS COCHES, ELIMINAMOS LA KEY DE CACHE REDIS
                if (cars.Count == 0)
                {
                    await this.cache.RemoveAsync("cochesfavoritos");
                }
                else
                {
                    // ALMACENAMOS DE NUEVO LOS COCHES SIN EL CAR ELIMINADO
                    string jsonCoches = JsonConvert.SerializeObject(cars);
                    DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    };
                    // ACTUALIZAMOS EL CACHE REDIS
                    await this.cache.SetStringAsync("cochesfavoritos", jsonCoches, options);
                }
            }
        }
    }
}
