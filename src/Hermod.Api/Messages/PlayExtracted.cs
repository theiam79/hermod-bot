using Hermod.BGStats.Models;

namespace Hermod.Api.Messages;

public record PlayExtracted(Play ParsedPlay, Guid? GroupId);
