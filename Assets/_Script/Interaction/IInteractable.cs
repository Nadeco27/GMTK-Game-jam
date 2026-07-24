using UnityEngine;

/// <summary>
/// Interface for any object that can be interacted with by the player.
/// Allows modular interaction implementations (Info items, Collectables, NPC dialogue, doors, switches, etc.).
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Message or prompt to display when player is in interaction range (e.g. "Tekan E untuk berinteraksi").
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Checks whether this object can currently be interacted with by the interactor.
    /// </summary>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// Executes the interaction logic when player presses the interact key.
    /// </summary>
    void Interact(GameObject interactor);
}
