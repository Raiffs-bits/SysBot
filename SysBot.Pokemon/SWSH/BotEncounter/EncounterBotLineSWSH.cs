using PKHeX.Core;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;

namespace SysBot.Pokemon;

public sealed class EncounterBotLineSWSH(PokeBotState Config, PokeTradeHub<PK8> Hub) : EncounterBotSWSH(Config, Hub)
{
    public override async Task RebootAndStop(CancellationToken t)
    {
        await ReOpenGame(new PokeTradeHubConfig(), t).ConfigureAwait(false);
        await HardStop().ConfigureAwait(false);
    }

    protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var attempts = await StepUntilEncounter(token).ConfigureAwait(false);
            if (attempts < 0) // aborted
                continue;

            Log($"Encounter found after {attempts} attempts! Checking details...");

            // Reset stick while we wait for the encounter to load.
            await ResetStick(token).ConfigureAwait(false);

            var pk = await ReadUntilPresent(WildPokemonOffset, 2_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (pk == null)
            {
                Log("Invalid data detected. Restarting loop.");

                // Flee and continue looping.
                await FleeToOverworld(token).ConfigureAwait(false);
                continue;
            }

            // Offsets are flickery so make sure we see it 3 times.
            for (int i = 0; i < 3; i++)
                await ReadUntilChanged(BattleMenuOffset, BattleMenuReady, 5_000, 0_100, true, token).ConfigureAwait(false);

            if (await HandleEncounter(pk, token).ConfigureAwait(false))
                return;

            Log("Running away...");
            await FleeToOverworld(token).ConfigureAwait(false);
        }
    }

    private async Task<int> StepUntilEncounter(CancellationToken token)
    {
        Log("Walking around until an encounter...");
        int attempts = 0;
        while (!token.IsCancellationRequested)
        {
            if (!await IsInBattle(token).ConfigureAwait(false))
            {
                switch (Hub.Config.EncounterSWSH.EncounteringType)
                {
                    case EncounterMode.VerticalLine:
                        await SetStick(LEFT, 0, -30000, 2_400, token).ConfigureAwait(false);
                        await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset

                        // Quit early if we found an encounter on first sweep.
                        if (await IsInBattle(token).ConfigureAwait(false))
                            break;

                        await SetStick(LEFT, 0, 30000, 2_400, token).ConfigureAwait(false);
                        await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset
                        break;

                    case EncounterMode.HorizontalLine:
                        await SetStick(LEFT, -30000, 0, 2_400, token).ConfigureAwait(false);
                        await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset

                        // Quit early if we found an encounter on first sweep.
                        if (await IsInBattle(token).ConfigureAwait(false))
                            break;

                        await SetStick(LEFT, 30000, 0, 2_400, token).ConfigureAwait(false);
                        await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset
                        break;
                }

                attempts++;
                if (attempts % 10 == 0)
                    Log($"Tried {attempts} times, still no encounters.");
            }

            if (await IsInBattle(token).ConfigureAwait(false))
                return attempts;
        }

        return -1; // aborted
    }
}
