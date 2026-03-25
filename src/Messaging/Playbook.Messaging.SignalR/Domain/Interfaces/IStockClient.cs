using Playbook.Messaging.SignalR.Domain.Models;

namespace Playbook.Messaging.SignalR.Domain.Interfaces;

/// <summary>
/// Defines the strongly-typed client-side contract for the Stock Ticker SignalR Hub.
/// This interface ensures compile-time safety when dispatching messages from the server to connected clients.
/// </summary>
/// <remarks>
/// By using a typed interface, the <see cref="T:Microsoft.AspNetCore.SignalR.Hub{IStockClient}"/> 
/// eliminates the need for "magic strings" during method invocation, reducing the risk of 
/// runtime desynchronization between the server and its consumers.
/// </remarks>
public interface IStockClient
{
    /// <summary>
    /// Dispatches a real-time market price update to the client.
    /// </summary>
    /// <param name="price">The <see cref="StockPrice"/> data transfer object containing the latest ticker information.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delivery of the price update.</returns>
    Task ReceivePriceUpdate(StockPrice price);

    /// <summary>
    /// Sends a general system notification or administrative message to the client.
    /// </summary>
    /// <param name="message">The plain-text notification content to be displayed or processed by the client.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delivery of the notification.</returns>
    Task ReceiveNotification(string message);
}
