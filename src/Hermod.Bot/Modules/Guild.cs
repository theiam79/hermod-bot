using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Bot.Modules
{
    [Group("settings", "update bot settings")]
    internal class Guild : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMediator _mediator;

        public Guild(IMediator mediator)
        {
            _mediator = mediator;
        }

        [SlashCommand("postchannel", "Set the post channel for sharing plays")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task UpdateChannelAsync(ITextChannel channel)
        {
            await UpdateSettingAsync(x => x.PostChannel = channel.Id);
        }

        //[SlashCommand("role", "Set a role that allows management of the bot settings")]
        //[RequireUserPermission(GuildPermission.ManageGuild)]
        //public async Task SetRoleAsync(IRole role)
        //{
        //    await UpdateSettingAsync(x => x.ManagementRole = role.Id);
        //}

        [SlashCommand("sharing", "Enable or Disable sharing for the guild")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task UpdateSharingAsync(bool sharing)
        {
            await UpdateSettingAsync(x => x.Sharing = sharing);
        }

        private async Task UpdateSettingAsync(Action<Core.Features.Guild.Settings.Edit.Command> action)
        {
            await DeferAsync();
            Core.Features.Guild.Settings.Edit.Command command = new()
            {
                GuildId = Context.Guild.Id
            };

            action(command);

            var result = await _mediator.Send(command);
            await FollowupAsync(result.IsSuccess ? "Setting updated successfully" : result.Errors.FirstOrDefault()?.Message);
        }
    }
}
