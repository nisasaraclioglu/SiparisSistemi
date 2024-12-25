using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SiparisSistemi.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiparisSistemi.Services
{
    public class OrderCleanupService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;

        public OrderCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 1 dakikada bir kontrol edilecek
            _timer = new Timer(CleanupOrders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void CleanupOrders(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 1 dakikadan eski ve "Pending" durumundaki sipariþleri bul
                var expiredOrders = dbContext.Orders
                    .Where(o => o.OrderStatus == "Pending" && o.OrderDate <= DateTime.Now.AddMinutes(-1))
                    .ToList();

                // Sipariþleri iptal et
                foreach (var order in expiredOrders)
                {
                    order.OrderStatus = "Cancelled";
                }

                dbContext.SaveChanges(); // Deðiþiklikleri veritabanýna kaydet
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
