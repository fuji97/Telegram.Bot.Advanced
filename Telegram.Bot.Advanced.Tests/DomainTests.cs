using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Tests.Infrastructure;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers <see cref="Data"/> equality/hash invariants and <see cref="TelegramChat"/> data-indexer + EF InMemory
/// persistence behavior.
/// </summary>
public sealed class DomainTests {
    private static TelegramChat MakeChat(long id = 1) => new(id) { Type = ChatType.Private };

    [Fact]
    public void Data_Equals_SameUserIdKeyValue_ReturnsTrue() {
        var chat = MakeChat();
        var a = new Data(chat, "key", "value");
        var b = new Data(chat, "key", "value");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Data_Equals_DifferentValue_ReturnsFalse() {
        var chat = MakeChat();
        var a = new Data(chat, "key", "value1");
        var b = new Data(chat, "key", "value2");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Data_EqualityOperator_BothNull_ReturnsTrue() {
        Data? a = null;
        Data? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void Data_EqualityOperator_OneNull_ReturnsFalse() {
        var chat = MakeChat();
        Data? a = new Data(chat, "key", "value");
        Data? b = null;

        Assert.False(a == b);
        Assert.False(b == a);
        Assert.True(a != b);
    }

    [Fact]
    public void Data_Equals_SameReferenceOnBothSides_ReturnsTrue() {
        // Exercises the ReferenceEquals(this, other) shortcut at the top of Equals/==. Note: for a non-null
        // instance, self-equality is unobservably different whether it is satisfied by the shortcut or by an
        // ordinary field-by-field comparison (the fields of a single object are always equal to themselves), so
        // this can only assert reflexivity - it cannot black-box-prove the shortcut bypassed field comparison.
        var chat = MakeChat();
        var data = new Data(chat, "key", "value");
        var alias = data;

        Assert.True(data.Equals(alias));
        Assert.True(data == alias);
    }

    [Fact]
    public void Data_GetHashCode_EqualInstances_ProduceSameHashCode() {
        var chat = MakeChat();
        var a = new Data(chat, "key", "value");
        var b = new Data(chat, "key", "value");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void TelegramChat_Indexer_GetSetAndUpdateExistingKey() {
        var chat = MakeChat();

        Assert.Null(chat["missing"]);

        chat["greeting"] = "hello";
        Assert.Equal("hello", chat["greeting"]);

        chat["greeting"] = "goodbye";
        Assert.Equal("goodbye", chat["greeting"]);
        Assert.Single(chat.Data);
    }

    [Fact]
    public void TelegramChat_AddData_DuplicateKey_ThrowsDuplicateDataKeyException() {
        var chat = MakeChat();
        chat.AddData("key", "value1");

        Assert.Throws<DuplicateDataKeyException>(() => chat.AddData("key", "value2"));
    }

    [Fact]
    public async Task TelegramChat_AddAsync_ThenGetAsync_PersistsAndRoundTrips() {
        var ct = TestContext.Current.CancellationToken;
        var dbName = Guid.NewGuid().ToString();

        await using (var writeContext = TestTelegramContext.Create(dbName)) {
            var chat = new TelegramChat(42) { Type = ChatType.Private, Username = "alice" };
            var added = await chat.AddAsync(writeContext, ct);
            Assert.True(added);
            await writeContext.SaveChangesAsync(ct);
        }

        await using var readContext = TestTelegramContext.Create(dbName);
        var fetched = await TelegramChat.GetAsync(readContext, 42, ct);

        Assert.NotNull(fetched);
        Assert.Equal(42, fetched!.Id);
        Assert.Equal("alice", fetched.Username);
    }

    [Fact]
    public async Task TelegramChat_AddAsync_ExistingId_ReturnsFalseWithoutDuplicating() {
        var ct = TestContext.Current.CancellationToken;
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestTelegramContext.Create(dbName);

        var first = new TelegramChat(7) { Type = ChatType.Private };
        Assert.True(await first.AddAsync(context, ct));
        await context.SaveChangesAsync(ct);

        var second = new TelegramChat(7) { Type = ChatType.Private };
        var added = await second.AddAsync(context, ct);

        Assert.False(added);
        Assert.Equal(1, await context.Users.CountAsync(u => u.Id == 7, ct));
    }
}
