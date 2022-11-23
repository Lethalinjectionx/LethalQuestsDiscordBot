using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using DSPlus.LethalQuestsBot.Helpers;
using AsciiTableGenerators;
using System.Data;

namespace DSPlus.LethalQuestsBot
{
    [Group("TOP")]
    public class TopCommand : BaseCommandModule
    {
        public readonly IConfiguration _configuration;
        public string connString;

        public TopCommand(IConfiguration configuration)
        {
            _configuration = configuration;
            connString = _configuration.GetConnectionString("myDb");
        }

        [GroupCommand]
        public async Task TopDefault(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var returnValue = "";

            foreach (var item in _configuration.GetSection("Commands").GetChildren())
            {
                if (returnValue == "")
                    returnValue = item.Key;
                else
                    returnValue = string.Format("{0}, {1}", returnValue, item.Key);
            }

            returnValue = string.Format("Usage `{1} top <option>`\nValid Options: {0}", returnValue, _configuration.GetValue<string>("prefix"));

            await ctx.RespondAsync(returnValue);
        }

        [GroupCommand]
        public async Task TopDefault(CommandContext ctx, string Show)
        {
            await ctx.TriggerTypingAsync();

            var returnValue = "";
            string SQLCommand = "";

            SQLCommand = _configuration.GetValue<string>("Commands:" + Show);
            if (SQLCommand == null)
            {
                await TopDefault(ctx);
                return;
            }

            using (var conn = new MySqlConnection(connString))
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

            await ctx.RespondAsync(returnValue);
        }
    }
}