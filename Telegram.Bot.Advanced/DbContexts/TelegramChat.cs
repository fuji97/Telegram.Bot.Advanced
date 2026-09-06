using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.DbContexts;

public sealed class TelegramChat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? State { get; set; } = null;
    public ChatType Type { get; set; }
    public ChatRole Role { get; set; } = ChatRole.User;
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; }
    public string? InviteLink { get; set; }
    public string? StickerSetName { get; set; }
    public bool? CanSetStickerSet { get; set; }
    public ICollection<Data> Data { get; set; } = [];
    public ICollection<NewsletterChat> NewsletterChats { get; set; } = [];

    public TelegramChat() {
    }

    public TelegramChat(long id) {
        Id = id;
    }

    public TelegramChat(Chat update) {
        Id = update.Id;
        Username = update.Username;
        Type = update.Type;
        Title = update.Title;
        FirstName = update.FirstName;
        LastName = update.LastName;
    }

    public void AddData(string key, string? value) {
        if (Data.Any(d => d.Key == key)) {
            throw new DuplicateDataKeyException();
        }

        Data.Add(new(this, key, value));
    }

    public void UpdateData(string key, string? value) {
        var data = Data.FirstOrDefault(d => d.Key == key);
        if (data is not null) {
            data.Value = value;
        }
        else {
            Data.Add(new(this, key, value));
        }
    }

    public string? this[string key]
    {
        get => Data.FirstOrDefault(d => d.Key == key)?.Value;
        set {
            var anotherName = Data.FirstOrDefault(d => d.Key == key);
            if (anotherName is not null) {
                anotherName.Value = value;
            }
            else
            {
                Data.Add(new(this, key, value));
            }
        }
    }

    public async Task<bool> AddAsync(TelegramContext context, CancellationToken cancellationToken = default) {
        var exists = await context.Users.AnyAsync(u => u.Id == Id, cancellationToken);
        if (exists) return false;
        await context.Users.AddAsync(this, cancellationToken);
        return true;
    }

    public static async Task<TelegramChat?> GetAsync(TelegramContext context, long id, CancellationToken cancellationToken = default) {
        var user = await context.Users
            .Include(chat => chat.Data)
            .Include(chat => chat.NewsletterChats)
            .ThenInclude(nc => nc.Newsletter)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user;
    }

    public ChatId ToChatId() => new(Id);

    public override bool Equals(object? obj) {
        if (obj is TelegramChat chat)
            return Id == chat.Id;
        return false;
    }

    private bool Equals(TelegramChat other) => Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public enum ChatRole {
    User = 0,
    Blocked = 1,
    Banned = 2,
    Moderator = 3,
    Administrator = 4
}
