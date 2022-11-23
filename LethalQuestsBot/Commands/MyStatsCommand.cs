using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using DSPlus.LethalQuestsBot.Helpers;

using System.Data;
using DSharpPlus.Entities;

namespace DSPlus.LethalQuestsBot
{
    [Group("MYSTATS")]
    public class MyStatsCommand : BaseCommandModule
    {
        public readonly IConfiguration _configuration;
        public string connString;

        public MyStatsCommand(IConfiguration configuration)
        {
            _configuration = configuration;
            connString = _configuration.GetConnectionString("myDb");
        }

        [GroupCommand]
        public async Task MyStatsDefault(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            //string username = Misc.GetUsername(ctx.User);
            ulong SteamID = GetSteamID(ctx.User).GetAwaiter().GetResult();
            string fields = _configuration.GetValue<string>("MyStats");
            if (fields.Length > 0 && fields != "*")
                fields = string.Format(", {0}", fields);

            string SQLCommand = string.Format("SELECT * FROM `lethalquests_stats` WHERE SteamID = '{0}'", SteamID);
            if (fields != "*")
                SQLCommand = string.Format("SELECT `SteamID`, `Name` AS `Player Name`, `TribeName` AS `Tribe`{0} FROM `lethalquests_stats` WHERE SteamID = '{1}'", fields, SteamID);

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            embed = new DiscordEmbedBuilder
                            {
                                Title = string.Format("{0} ({1})", reader[1], reader[2]),
                                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = ctx.User.GetAvatarUrl(DSharpPlus.ImageFormat.Auto, 128) },
                                Color = new DiscordColor(_configuration.GetValue<string>("EmbedColor"))
                            };

                            int loops = reader.FieldCount;
                            if (loops > 28)
                                loops = 28;

                            for (int i = 3; i < loops; i++)
                            {
                                if (reader[i] != null)
                                    embed.AddField(reader.GetColumnSchema()[i].ColumnName, string.Format("{0}", reader[i]), true);
                            }
                        }
                    }
                }
            }

            if (embed.Fields.Count == 0)
                await ctx.RespondAsync(_configuration.GetValue<string>("NoResults", "No Results Found!"));
            else
                await ctx.RespondAsync(embed);
        }

        public async Task<ulong> GetSteamID(DiscordUser User)
        {
            ulong SteamID = 0;
            string UserName = Misc.GetUsername(User);
            string SQLCommand = string.Format("SELECT SteamID FROM `lethalquests_discord` WHERE Discord = '{0}' OR DiscordID = {1}", UserName, User.Id);
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            SteamID = reader.GetUInt64(0);
                    }
                }
            }

            return SteamID;
        }
    }
}