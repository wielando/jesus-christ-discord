using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Victoria.Node;

class Program
{
    private DiscordSocketClient _client;
    private ITextChannel _announcementChannel; // Der Channel, in den der Bot die Nachricht sendet
    private IUserMessage _announcementMessage; // Die Nachricht, auf die der Bot reagieren wird
    private IVoiceChannel _createTemporaryVC;

    private ulong _serverId = 1164859455368863765;

    private ulong _selectRoleChId = 1164863623756259428;
    private ulong _createTemporaryVCId = 1164873784801632256;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += LogAsync;

        _client.Ready += OnBotReady;

        await _client.LoginAsync(TokenType.Bot, "MTE2NDg1MTQ0NTY3MDA4NDY4MA.GEGjlp.kfGa2uBggQES7V2kRB05nFCIBfhn8yRPZjS1Ck");
        await _client.StartAsync();

        await Task.Delay(-1);
    }


    private async Task OnBotReady()
    {
        Console.WriteLine("Bot is connected.");

        _announcementChannel = _client.GetGuild(_serverId).GetTextChannel(_selectRoleChId);
        _createTemporaryVC = _client.GetGuild(_serverId).GetVoiceChannel(_createTemporaryVCId);

        _client.ReactionAdded += HandleReactionAdded;
        _client.ReactionRemoved += HandleReactionRemoved;
        _client.UserVoiceStateUpdated += HandleUserVoiceStateUpdated;


        var timer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds); // Überprüfung alle 5 Minuten
        timer.Elapsed += HandleEmptyVoiceChannels;
        timer.Start();

        _announcementMessage = await _announcementChannel.SendMessageAsync("Reagiere auf folgende Reaktionen, um die entsprechende Rolle zu erhalten!\n\nFröhlich :smiley:\nTraurig :cry:");
        await _announcementMessage.AddReactionsAsync(new IEmote[] { new Emoji("🙂"), new Emoji("😢") });
    }

    private async Task HandleUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        if (newState.VoiceChannel != null && newState.VoiceChannel.Id == _createTemporaryVCId)
        {
            // Nutzer ist dem "Create Temporary VC" beigetreten
            var userNickname = (user as IGuildUser)?.Nickname ?? user.Username;
            var channelName = $"{userNickname}'s TMP VC";

            // Erstelle einen neuen Voice Channel
            var newVC = await newState.VoiceChannel.Guild.CreateVoiceChannelAsync(channelName, properties =>
            {
                properties.Bitrate = newState.VoiceChannel.Bitrate;
                properties.UserLimit = newState.VoiceChannel.UserLimit;
                properties.CategoryId = 1164878807442919504;
            });

            // Bewege den Nutzer aus dem alten Voice Channel
            if (oldState.VoiceChannel != null)
            {
                var voiceChannel = oldState.VoiceChannel as IVoiceChannel;
                if (voiceChannel != null)
                {
                    await oldState.VoiceChannel.Guild.GetUser(user.Id).ModifyAsync(properties =>
                    {
                        properties.Channel = Optional.Create<IVoiceChannel>(null);
                    });
                }
            }

            // Bewege den Nutzer in den neuen Voice Channel
            await newState.VoiceChannel.Guild.GetUser(user.Id).ModifyAsync(properties =>
            {
                properties.Channel = Optional.Create<IVoiceChannel>(newVC);
            });
        }
    }


    private async void HandleEmptyVoiceChannels(object sender, System.Timers.ElapsedEventArgs e)
    {
        var guild = _client.GetGuild(_serverId);
        var emptyVoiceChannels = guild.VoiceChannels.Where(vc => !vc.ConnectedUsers.Any() && vc.CategoryId == 1164878807442919504).ToList();

        await Console.Out.WriteLineAsync("Looking for empty temp channels");

        foreach (var emptyVoiceChannel in emptyVoiceChannels)
        {
            await emptyVoiceChannel.DeleteAsync();
        }
    }

    private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> msg, SocketReaction reaction)
    {
        if (reaction.MessageId == _announcementMessage.Id)
        {
            var user = _client.GetGuild(_serverId).GetUser(reaction.UserId);
            if (user != null)
            {
                if (reaction.Emote.Name == "🙂")
                {
                    var role = _client.GetGuild(_serverId).Roles.FirstOrDefault(r => r.Name == "Fröhlich");
                    if (role != null)
                    {
                        await user.AddRoleAsync(role);
                    }
                }
                else if (reaction.Emote.Name == "😢")
                {
                    var role = _client.GetGuild(_serverId).Roles.FirstOrDefault(r => r.Name == "Traurig");
                    if (role != null)
                    {
                        await user.AddRoleAsync(role);
                    }
                }
            }
        }
    }

    private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> msg, SocketReaction reaction)
    {
        if (reaction.MessageId == _announcementMessage.Id)
        {
            var user = _client.GetGuild(_serverId).GetUser(reaction.UserId);
            if (user != null)
            {
                if (reaction.Emote.Name == "🙂")
                {
                    var role = _client.GetGuild(_serverId).Roles.FirstOrDefault(r => r.Name == "Fröhlich");
                    if (role != null)
                    {
                        await user.RemoveRoleAsync(role);
                    }
                }
                else if (reaction.Emote.Name == "😢")
                {
                    var role = _client.GetGuild(_serverId).Roles.FirstOrDefault(r => r.Name == "Traurig");
                    if (role != null)
                    {
                        await user.RemoveRoleAsync(role);
                    }
                }
            }
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }
}