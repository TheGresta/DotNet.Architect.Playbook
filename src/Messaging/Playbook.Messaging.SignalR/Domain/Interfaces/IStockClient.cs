using Playbook.Messaging.SignalR.Domain.Models;

namespace Playbook.Messaging.SignalR.Domain.Interfaces;

public interface IStockClient
{
    Task ReceivePriceUpdate(StockPrice price);
    Task ReceiveNotification(string message);
}
