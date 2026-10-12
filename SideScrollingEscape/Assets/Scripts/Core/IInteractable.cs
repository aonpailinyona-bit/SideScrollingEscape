using UnityEngine;

namespace SideScrollingEscape.Core
{
    /// <summary>
    /// Implemented by world objects the player can interact with (exit doors,
    /// switches, etc.). The player's interactor invokes these on input.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Whether the interactor is currently allowed to interact with this object.
        /// </summary>
        /// <param name="interactor">The GameObject attempting the interaction.</param>
        /// <returns>True if interaction is permitted.</returns>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Performs the interaction.
        /// </summary>
        /// <param name="interactor">The GameObject performing the interaction.</param>
        void Interact(GameObject interactor);

        /// <summary>
        /// Short prompt describing the interaction (e.g. "Press E to open").
        /// </summary>
        /// <returns>Prompt text for UI display.</returns>
        string GetPrompt();
    }
}
