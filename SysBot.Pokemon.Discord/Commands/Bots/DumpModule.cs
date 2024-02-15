using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Dump trades")]
public class DumpModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("dump")]
    [Alias("d")]
    [Summary("Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesDump))]
    public async Task DumpAsync(int code)
    {
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();
        await QueueHelper<T>.AddToQueueAsync(
            Context,
            code,
            Context.User.Username,
            sig,
            new T(),
            PokeRoutineType.Dump,
            PokeTradeType.Dump,
            Context.User,
            isBatchTrade: false,
            batchTradeNumber: 1,
            totalBatchTrades: 1,
            formArgument: 0,
            isMysteryEgg: false,
            lgcode: lgcode);

        // Delete the command message after 2 seconds
        await Task.Delay(2000);
        await Context.Message.DeleteAsync();
    }

    [Command("dump")]
    [Alias("d")]
    [Summary("Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesDump))]
    public async Task DumpAsync([Summary("Trade Code")][Remainder] string code)
    {
        int tradeCode = Util.ToInt32(code);
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sig, new T(), PokeRoutineType.Dump, PokeTradeType.Dump);
    }

    [Command("dump")]
    [Alias("d")]
    [Summary("Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesDump))]
    public async Task DumpAsync()
    {
        var code = Info.GetRandomTradeCode();
        await DumpAsync(code);
    }

    [Command("dumpList")]
    [Alias("dl", "dq")]
    [Summary("Prints the users in the Dump queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Dump);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}
