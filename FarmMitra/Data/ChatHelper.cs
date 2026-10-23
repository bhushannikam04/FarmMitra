using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Data;

public static class ChatHelper
{
    public const int MaxLength = 2000;

    // One conversation per buyer + farmer pair.
    public static async Task<Conversation> FindOrCreateAsync(ApplicationDbContext db, string buyerId, string farmerId, int? productId)
    {
        var existing = await db.Conversations.FirstOrDefaultAsync(c => c.BuyerId == buyerId && c.FarmerId == farmerId);
        if (existing != null)
        {
            if (productId.HasValue && existing.ProductId != productId)
            {
                existing.ProductId = productId;
                await db.SaveChangesAsync();
            }
            return existing;
        }

        var conversation = new Conversation { BuyerId = buyerId, FarmerId = farmerId, ProductId = productId };
        db.Conversations.Add(conversation);
        try
        {
            await db.SaveChangesAsync();
            return conversation;
        }
        catch (DbUpdateException)
        {
            // Another request created it a moment ago (unique index): use that one.
            db.Entry(conversation).State = EntityState.Detached;
            return await db.Conversations.FirstAsync(c => c.BuyerId == buyerId && c.FarmerId == farmerId);
        }
    }

    public static Task<List<InboxRow>> GetInboxAsync(ApplicationDbContext db, string userId) =>
        db.Conversations
            .Where(c => c.BuyerId == userId || c.FarmerId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Take(100)
            .Select(c => new InboxRow
            {
                Id = c.Id,
                OtherName = c.BuyerId == userId ? c.Farmer!.FullName : c.Buyer!.FullName,
                ProductName = c.Product != null ? c.Product.Name : null,
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt)
                                .Select(m => m.IsHidden ? "Message removed" : m.Body)
                                .FirstOrDefault(),
                LastMessageAt = c.LastMessageAt,
                Unread = c.Messages.Count(m => m.SenderId != userId && !m.IsRead),
                IsClosed = c.IsClosed
            })
            .ToListAsync();

    public static Task<int> UnreadCountAsync(ApplicationDbContext db, string userId) =>
        db.Messages.CountAsync(m => m.SenderId != userId && !m.IsRead
                                    && (m.Conversation!.BuyerId == userId || m.Conversation.FarmerId == userId));

    // participantUserId == null means an admin is watching (read-only, nothing is marked as read).
    public static async Task<ThreadViewModel?> GetThreadAsync(ApplicationDbContext db, int id, string? participantUserId, string area)
    {
        var query = db.Conversations.Where(c => c.Id == id);
        if (participantUserId != null)
            query = query.Where(c => c.BuyerId == participantUserId || c.FarmerId == participantUserId);

        var vm = await query.Select(c => new ThreadViewModel
        {
            Id = c.Id,
            Area = area,
            BuyerId = c.BuyerId,
            BuyerName = c.Buyer!.FullName,
            FarmerId = c.FarmerId,
            FarmerName = c.Farmer!.FullName,
            ProductId = c.ProductId,
            ProductName = c.Product != null ? c.Product.Name : null,
            IsClosed = c.IsClosed,
            IsReported = c.IsReported,
            ReportReason = c.ReportReason
        }).FirstOrDefaultAsync();

        if (vm == null) return null;

        vm.ViewerId = participantUserId;
        vm.IsAdmin = participantUserId == null;

        vm.Messages = await db.Messages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageRow
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender!.FullName,
                Body = m.Body,
                SentAt = m.SentAt,
                IsHidden = m.IsHidden
            })
            .ToListAsync();

        // Removed messages never reach a participant's browser.
        if (!vm.IsAdmin)
            foreach (var m in vm.Messages.Where(m => m.IsHidden))
                m.Body = "";

        if (participantUserId != null)
        {
            await db.Messages
                .Where(m => m.ConversationId == id && m.SenderId != participantUserId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        }

        return vm;
    }

    // Returns an error message, or null when the message was sent.
    public static async Task<string?> SendAsync(ApplicationDbContext db, int conversationId, string userId, string? body)
    {
        body = body?.Trim();
        if (string.IsNullOrEmpty(body)) return "Type a message first.";
        if (body.Length > MaxLength) return $"Messages can be up to {MaxLength} characters.";

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && (c.BuyerId == userId || c.FarmerId == userId));
        if (conversation == null) return "Conversation not found.";
        if (conversation.IsClosed) return "This conversation was closed by an administrator.";

        var now = DateTime.UtcNow;
        db.Messages.Add(new Message { ConversationId = conversationId, SenderId = userId, Body = body, SentAt = now });
        conversation.LastMessageAt = now;
        await db.SaveChangesAsync();
        return null;
    }

    public static async Task<bool> ReportAsync(ApplicationDbContext db, int conversationId, string userId, string? reason)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && (c.BuyerId == userId || c.FarmerId == userId));
        if (conversation == null) return false;

        reason = reason?.Trim();
        if (!string.IsNullOrEmpty(reason) && reason.Length > 300) reason = reason[..300];

        conversation.IsReported = true;
        conversation.ReportReason = string.IsNullOrEmpty(reason) ? "No reason given" : reason;
        await db.SaveChangesAsync();
        return true;
    }
}
