namespace Reward.Application.Ports;

/// <summary>
/// Asks Player, which owns players and game session participants, whether an authenticated
/// user took part in a dungeon run.
/// </summary>
public interface IPlayerParticipationClient
{
    /// <param name="externalUserId">Trusted gateway identity (X-User-Id, Keycloak subject).</param>
    /// <exception cref="Reward.Application.Exceptions.ExternalServiceUnavailableException">
    /// Player cannot answer: no participation can be assumed.
    /// </exception>
    Task<bool> IsRunParticipantAsync(
        string externalUserId,
        Guid dungeonRunId,
        CancellationToken cancellationToken
    );
}
