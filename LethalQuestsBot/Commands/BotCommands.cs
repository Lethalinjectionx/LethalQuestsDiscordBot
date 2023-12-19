using MySqlConnector;
using AsciiTableGenerators;
using System.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using DSPlus.LethalQuestsBot.Helpers;

namespace DSPlus.LethalQuestsBot
{
    [SlashCommandGroup("LQ", "Lethal Quests Commands", true)]
    public class BotCommands : ApplicationCommandModule
    {
        public readonly IConfiguration _configuration;
        public string _connString;
        public BotCommands(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("myDb");
        }

        [SlashCommand("top", "Get top stats")]
        public async Task TopDefault(InteractionContext ctx, [ChoiceProvider(typeof(TopCommandProvider))][Option("option", "option")] string option)
        {
            var returnValue = "";
            string SQLCommand = "";

            SQLCommand = _configuration.GetValue<string>("Commands:" + option);
            if (SQLCommand == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Option was not found!").AsEphemeral());
                return;
            }

            using (var conn = new MySqlConnection(_connString))
            {
                await conn.OpenAsync();

                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        DataTable dtData = new DataTable("Data");
                        DataTable dtSchema = new DataTable("Schema");
                        dtSchema = reader.GetSchemaTable();
                        dtData.Columns.Add("#");
                        foreach (DataRow schemarow in dtSchema.Rows)
                        {
                            dtData.Columns.Add(schemarow.ItemArray[0].ToString());
                        }
                        int count = 1;
                        while (reader.Read())
                        {
                            object[] ColArray = new object[reader.FieldCount + 1];
                            ColArray[0] = count;
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader[i] != null) ColArray[i + 1] = reader[i];
                            }
                            dtData.LoadDataRow(ColArray, true);
                            count++;
                        }

                        var asciiText = AsciiTableGenerator.CreateAsciiTableFromDataTable(dtData);
                        returnValue = string.Format("```d\n{0}\n```", asciiText.ToString());
                    }
                }
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(returnValue));
        }

        [SlashCommand("register", "Link in-game to discord")]
        public async Task RegisterDefault(InteractionContext ctx, [Option("code", "6 digit code provided in-game")] string Code)
        {
            var returnValue = "";
            string SQLCommand = "";

            if (Code == null || Code.Length != 6)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Invalid Code Provided!").AsEphemeral());
                return;
            }

            SQLCommand = string.Format("SELECT eos_id FROM `lethalquestsascended_discord` WHERE Code = '{0}'", Code);
            using (var conn = new MySqlConnection(_connString))
            {
                await conn.OpenAsync();

                string eos_id = "";
                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            eos_id = reader.GetString(0);
                    }
                }

                string username = Misc.GetUsername(ctx.User);
                SQLCommand = string.Format("UPDATE `lethalquestsascended_discord` SET Discord = '{0}', Code='', DiscordID = {2} WHERE eos_id = '{1}'", username, eos_id, ctx.User.Id);
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

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(returnValue));
        }

        [SlashCommand("mystats", "Get my stats")]
        public async Task MyStatsDefault(InteractionContext ctx)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            string EOSID = await GetEOSID(ctx.User);
            string fields = _configuration.GetValue<string>("MyStats");
            if (fields.Length > 0 && fields != "*")
                fields = string.Format(", {0}", fields);

            string SQLCommand = string.Format("SELECT * FROM `lethalquestsascended_stats` WHERE eos_id = '{0}'", EOSID);
            if (fields != "*")
                SQLCommand = string.Format("SELECT `eos_id`, `Name` AS `Player Name`, `TribeName` AS `Tribe`{0} FROM `lethalquestsascended_stats` WHERE eos_id = '{1}'", fields, EOSID);

            using (var conn = new MySqlConnection(_connString))
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
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(_configuration.GetValue<string>("NoResults", "No Results Found!")));
            else
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        public async Task<string> GetEOSID(DiscordUser User)
        {
            string EOSID = "";
            string UserName = Misc.GetUsername(User);
            string SQLCommand = string.Format("SELECT eos_id FROM `lethalquestsascended_discord` WHERE Discord = '{0}' OR DiscordID = {1}", UserName, User.Id);
            using (var conn = new MySqlConnection(_connString))
            {
                await conn.OpenAsync();

                // Retrieve all rows
                using (var cmd = new MySqlCommand(SQLCommand, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                            EOSID = reader.GetString(0);
                    }
                }
            }

            return EOSID;
        }
    }

    //ChoiceProvider choices
    public class TopCommandProvider : IChoiceProvider
    {
        public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            return GlobalConfiguration.Configuration.GetSection("Commands").GetChildren().Select(section => new DiscordApplicationCommandOptionChoice(section.Key, section.Key)).ToList();
        }
    }
}