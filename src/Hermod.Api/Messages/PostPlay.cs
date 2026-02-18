namespace Hermod.Api.Messages;

/// <summary>
/// Dispatched after a play is persisted. Instructs the embed posting handler to
/// check group settings and post a Discord embed if appropriate.
/// </summary>
public record PostPlay(Guid PlayId, Guid GroupId);
