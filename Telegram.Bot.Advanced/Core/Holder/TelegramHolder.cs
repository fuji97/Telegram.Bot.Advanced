using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Telegram.Bot.Advanced.Core.Holder;

public sealed class TelegramHolder : ITelegramHolder {
    private readonly FrozenDictionary<string, ITelegramBotData> _bots;

    public TelegramHolder(IEnumerable<ITelegramBotData> bots) {
        ArgumentNullException.ThrowIfNull(bots);

        var map = new Dictionary<string, ITelegramBotData>(StringComparer.Ordinal);
        foreach (var bot in bots) {
            if (!map.TryAdd(bot.Endpoint, bot)) {
                throw new ArgumentException($"Duplicate bot endpoint '{bot.Endpoint}'.", nameof(bots));
            }
        }

        _bots = map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public bool TryGet(string endpoint, [NotNullWhen(true)] out ITelegramBotData? bot) => _bots.TryGetValue(endpoint, out bot);

    public IEnumerator<ITelegramBotData> GetEnumerator() => ((IEnumerable<ITelegramBotData>)_bots.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
