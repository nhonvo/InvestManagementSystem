using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class NotificationService(IUnitOfWork unitOfWork) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<NotificationResponse> CreateAsync(
        Guid userId, 
        string message, 
        NotificationType type = NotificationType.System,
        NotificationSeverity severity = NotificationSeverity.Info,
        string? symbol = null, 
        Guid? alertRuleId = null, 
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Type = type,
            Severity = severity,
            TickerSymbol = symbol,
            AlertRuleId = alertRuleId,
            IsRead = false
        };

        await _unitOfWork.Notifications.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new NotificationResponse(
            notification.Id,
            notification.Message,
            notification.TickerSymbol,
            notification.Type,
            notification.Severity,
            notification.IsRead,
            notification.CreatedAt);
    }

    public async Task<PagedResult<NotificationResponse>> GetPagedAsync(string userId, bool onlyUnread, int page, int pageSize, CancellationToken ct)
    {
        var result = await _unitOfWork.Notifications.GetByUserPagedAsync(userId, onlyUnread, page, pageSize, ct);

        return new PagedResult<NotificationResponse>
        {
            Items = result.Items.Select(n => new NotificationResponse(
                n.Id, 
                n.Message, 
                n.TickerSymbol, 
                n.Type, 
                n.Severity, 
                n.IsRead, 
                n.CreatedAt)),
            TotalItems = result.TotalItems,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct)
    {
        return await _unitOfWork.Notifications.GetUnreadCountAsync(userId, ct);
    }

    public async Task MarkReadAsync(Guid id, string userId, CancellationToken ct)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id, ct);
        if (notification != null && notification.UserId == Guid.Parse(userId))
        {
            notification.IsRead = true;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<int> MarkAllReadAsync(string userId, CancellationToken ct)
    {
        return await _unitOfWork.Notifications.MarkAllReadAsync(userId, ct);
    }

    public async Task DismissAsync(Guid id, string userId, CancellationToken ct)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id, ct);
        if (notification != null && notification.UserId == Guid.Parse(userId))
        {
            await _unitOfWork.Notifications.DeleteAsync(notification, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
