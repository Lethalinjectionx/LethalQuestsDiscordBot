using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using DSPlus.LethalQuestsBot.Helpers;

using System.Data;
using System;

namespace DSPlus.LethalQuestsBot
{
    [Group("REGISTER")]
    public class RegisterCommand : BaseCommandModule
    {
        public readonly IConfiguration _configuration;
        public string connString;

        public RegisterCommand(IConfiguration configuration)
        {
            _configuration = configuration;
            connString = _configuration.GetConnectionString("myDb");
        }

        [GroupCommand]
        public async Task RegisterDefault(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var returnValue = "You need a valid 6 letter code from in game to complete your registration.";

            await ctx.RespondAsync(returnValue);
        }

        [GroupCommand]
        public async Task RegisterDefault(CommandContext ctx, string Code)
        {
            await ctx.TriggerTypingAsync();

            var returnValue = "";
            string SQLCommand = "";

            if (Code == null || Code.Length != 6)
            {
                await RegisterDefault(ctx);
                return;
            }

            SQLCommand = string.Format("SELECT SteamID FROM `lethalquests_discord` WHERE Code = '{0}'", Code);
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                ulong SteamID = 0;
                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            SteamID = reader.GetUInt64(0);
                    }
                }

                string username = Misc.GetUsername(ctx.User);
                SQLCommand = string.Format("UPDATE `lethalquests_discord` SET Discord = '{0}', Code='', DiscordID = {2} WHERE SteamID = '{1}'", username, SteamID, ctx.User.Id);
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    try
                    {
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows == 1)
                            returnValue = "You are now registered!";
                        else
                            returnValue = "Invalid registration code!";
                    }
                    catch (Exception)
                    {
                        returnValue = "Discord user was already in use, contact the server admin!";
                    }
                }
            }

            await ctx.RespondAsync(returnValue);
        }
    }
}